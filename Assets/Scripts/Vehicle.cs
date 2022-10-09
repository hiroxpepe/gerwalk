/*
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// vehicle controller
    /// @author h.adachi
    /// </summary>
    public class Vehicle : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        float _jump_power = 15.0f;

        [SerializeField]
        float _rotational_speed = 10.0f;

        [SerializeField]
        float _pitch_speed = 5.0f;

        [SerializeField]
        float _roll_speed = 5.0f;

        [SerializeField]
        float _flight_power = 5.0f;

        [SerializeField]
        float _forward_speed_limit = 1.1f;

        [SerializeField]
        float _run_speed_limit = 3.25f;

        [SerializeField]
        float _backward_speed_limit = 0.75f;

        [SerializeField]
        SimpleAnimation _simple_anime;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // static Fields [noun, adjectives] 

        static float _total_power_factor_value = 1.5f;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        GameSystem _game_system;

        DoUpdate _do_update;

        DoFixedUpdate _do_fixed_update;

        Acceleration _acceleration;

        Energy _energy;

        System.Diagnostics.Stopwatch _flight_stopwatch = new();

        float _flight_time, _air_speed, _vertical_speed, _total, _power = 0f;

        bool _use_lift_spoiler = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// current speed of the vehicle object to flight.
        /// </summary>
        public float airSpeed {
            get => _air_speed;
            set { _air_speed = value; Updated?.Invoke(this, new(nameof(airSpeed))); }
        }

        /// current vertical speed of the vehicle object to flight.
        /// </summary>
        public float verticalSpeed { 
            get => _vertical_speed;
            set { _vertical_speed = value; Updated?.Invoke(this, new(nameof(verticalSpeed))); }
        }

        /// <summary>
        /// elapsed time after takeoff.
        /// </summary>
        public float flightTime { 
            get => _flight_time;
            set { _flight_time = value; Updated?.Invoke(this, new(nameof(flightTime))); }
        }

        /// <summary>
        /// total energy.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float total {
            get => _total;
            set { _total = value; Updated?.Invoke(this, new(nameof(total))); }
        }

        /// <summary>
        /// current power of the vehicle to flight.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float power { 
            get => _power;
            set { _power = value; Updated?.Invoke(this, new(nameof(power))); }
        }

        /// <summary>
        /// use or not use lift spoiler.
        /// </summary>
        public bool useLiftSpoiler { 
            get => _use_lift_spoiler;
            set { _use_lift_spoiler = value; Updated?.Invoke(this, new(nameof(useLiftSpoiler))); }
        }

        /// <summary>
        /// transform position.
        /// </summary>
        public Vector3 position {
            get => transform.position;
            set { transform.position = value; Updated?.Invoke(this, new(nameof(position))); }
        }

        /// <summary>
        /// transform rotation.
        /// </summary>
        public Quaternion rotation {
            get => transform.rotation;
            set { transform.rotation = value; Updated?.Invoke(this, new(nameof(rotation))); }
        }

        /// <summary>
        /// value of heading angle.
        /// </summary>
        public float heading {
            get {
                // vector.y
                //   0 -> 360
                Vector3 angle = transform.localEulerAngles;
                return angle.y;
            }
        }

        /// <summary>
        /// value of roll angle.
        /// </summary>
        public float roll {
            get {
                // vector.z
                //   right: 360 -> 180
                //   left :   0 -> 180
                Vector3 angle = transform.localEulerAngles;
                return angle.z;
            }
        }

        /// <summary>
        /// value of bank angle.
        /// </summary>
        public float bank {
            get {
                // roll
                //   right: 360 -> 180
                //   left :   0 -> 180
                if (roll <= 180) {
                    return roll;
                } else if (roll > 180) {
                    return -(roll - 360f);
                }
                return 0f;
            }
        }

        /// <summary>
        /// value of pitch angle.
        /// </summary>
        public float pitch {
            get {
                // vector.x
                //   positive: 360 -> 270 = 270 -> 360
                //   negative:   0 ->  90 =  90 ->   0
                //     to pitch
                //   positive:   0 ->  90 =  90 ->   0
                //   negative:  -0 -> -90 = -90 ->  -0
                Vector3 angle = transform.localEulerAngles;
                if (angle.x >= 270) {
                    return 360f - angle.x;
                }
                else if (angle.x <= 90) {
                    return -angle.x;
                }
                return 0f;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action? OnFlight;

        public event Action? OnGrounded;

        public event Action? OnGainEnergy;

        public event Action? OnLoseEnergy;

        public event Action? OnStall;

        /// <summary>
        /// changed event handler.
        /// </summary>
        public event Changed? Updated;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = gameObject.GetGameSystem();
            _do_update = DoUpdate.GetInstance();
            _do_fixed_update = DoFixedUpdate.GetInstance();
            _acceleration = Acceleration.GetInstance(_forward_speed_limit, _run_speed_limit, _backward_speed_limit);
            _energy = Energy.GetInstance(_flight_power);
            _energy.Updated += onChanged;
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            const float ADD_FORCE_VALUE = 12.0f;

            // set game mode
            switch (_game_system.mode) {
                case Envelope.MODE_EASY: _total_power_factor_value = 1.0f; break;
                case Envelope.MODE_NORMAL: _total_power_factor_value = 1.5f; break;
                case Envelope.MODE_HARD: _total_power_factor_value = 2.0f; break;
            }

            /// <remarks>
            /// Rigidbody should be only used in FixedUpdate.
            /// </remarks>
            Rigidbody rb = transform.GetRigidbody();

            // FIXME: to integrate with Energy function.
            this.FixedUpdateAsObservable().Subscribe(_ => {
                _acceleration.previousSpeed = _acceleration.currentSpeed;// hold previous speed.
                _acceleration.currentSpeed = rb.velocity.magnitude; // get speed.
            });

            // get vehicle speed for flight.
            Vector3 prev_position = transform.position;
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                const float VALUE_MS_TO_KMH = 3.6f; // m/s -> km/h
                airSpeed = ((transform.position - prev_position) / Time.deltaTime).magnitude * VALUE_MS_TO_KMH; 
                verticalSpeed = ((transform.position.y - prev_position.y) / Time.deltaTime); // m/s
                prev_position = transform.position;
            });

            #region for grounded player

            /// <summary>
            /// idol.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && !_up_button.isPressed && !_down_button.isPressed).Subscribe(_ => {
                //_simpleAnime.Play("Default");
                _do_fixed_update.ApplyIdol();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.idol).Subscribe(_ => {
                rb.useGravity = true;
            });

            /// <summary>
            /// walk.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && _up_button.isPressed && !_y_button.isPressed).Subscribe(_ => {
                /*_simpleAnime.Play("Walk");*/
                _do_fixed_update.ApplyWalk();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.walk && _acceleration.canWalk).Subscribe(_ => {
                const float ADJUST_VALUE = 7.5f;
                rb.AddFor​​ce(transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, ForceMode.Acceleration);
                _do_fixed_update.CancelWalk();
            });

            /// <summary>
            /// run.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && _up_button.isPressed && _y_button.isPressed).Subscribe(_ => {
                /*_simpleAnime.Play("Run");*/
                _do_fixed_update.ApplyRun();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.run && _acceleration.canRun).Subscribe(_ => {
                const float ADJUST_VALUE = 7.5f;
                rb.AddFor​​ce(transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, ForceMode.Acceleration);
                _do_fixed_update.CancelRun();
            });

            /// <summary>
            /// backward.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && _down_button.isPressed).Subscribe(_ => {
                /*_simpleAnime.Play("Walk");*/
                _do_fixed_update.ApplyBackward();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.backward && _acceleration.canBackward).Subscribe(_ => {
                const float ADJUST_VALUE = 7.5f;
                rb.AddFor​​ce(-transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, ForceMode.Acceleration);
                _do_fixed_update.CancelBackward();
            });

            /// <summary>
            /// jump.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _b_button.wasPressedThisFrame && _do_update.grounded).Subscribe(_ => {
                _do_update.grounded = false;
                //_simpleAnime.Play("Jump");
                _do_fixed_update.ApplyJump();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.jump).Subscribe(_ => {
                const float ADJUST_VALUE = 2.0f;
                rb.useGravity = true;
                rb.AddRelativeFor​​ce(up * _jump_power * ADD_FORCE_VALUE * ADJUST_VALUE, ForceMode.Acceleration);
                _do_fixed_update.CancelJump();
            });

            /// <summary>
            /// freeze.
            /// </summary>
            this.OnCollisionStayAsObservable().Where(x => x.LikeBlock() && (_up_button.isPressed || _down_button.isPressed) && _acceleration.freeze).Subscribe(_ => {
                var reach = getReach();
                if (_do_update.grounded && (reach < 0.5d || reach >= 0.99d)) {
                    moveLetfOrRight(getDirection(transform.forward));
                }
                else if (reach >= 0.5d && reach < 0.99d) {
                    rb.useGravity = false;
                    moveTop();
                }
            });

            /// <summary>
            /// when touching blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(x => x.LikeBlock()).Subscribe(x => {
                if (!isHitSide(x.gameObject)) {
                    _do_update.grounded = true;
                    rb.useGravity = true;
                }
            });

            /// <summary>
            /// when leaving blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionExitAsObservable().Where(x => x.LikeBlock()).Subscribe(x => {
                rb.useGravity = true;
            });

            #endregion

            /// <summary>
            /// flight.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded).Subscribe(_ => {
                _energy.speed = airSpeed;
                _energy.altitude = transform.position.y - 0.5f; // 0.5 is half vehicle height.
                _do_fixed_update.ApplyFlight();
                OnFlight?.Invoke(); // call event handler.
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.flight).Subscribe(_ => {
                const float FALL_VALUE = 5.0f; // 5.0 -> -5m/s
                rb.useGravity = false;
                rb.velocity = transform.forward * _energy.GetCalculatedPower();
                if (useLiftSpoiler) {
                    var velocity = rb.velocity;
                    rb.velocity = new Vector3(velocity.x, velocity.y - FALL_VALUE, velocity.z);
                }
                _do_fixed_update.CancelFlight();
                _flight_stopwatch.Start();
            });

            /// <summary>
            /// gain energy.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _b_button.wasPressedThisFrame && !_do_update.grounded && _game_system.usePoint).Subscribe(_ => {
                OnGainEnergy?.Invoke();
                _energy.Gain();
            });

            /// <summary>
            /// lose energy.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _y_button.wasPressedThisFrame && !_do_update.grounded && _game_system.usePoint).Subscribe(_ => {
                OnLoseEnergy?.Invoke();
                _energy.Lose();
            });

            /// <summary>
            /// use or not use lift spoiler.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _x_button.wasPressedThisFrame && !_do_update.grounded).Subscribe(_ => {
                useLiftSpoiler = !useLiftSpoiler;
            });

            /// <summary>
            /// rotate(yaw).
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded).Subscribe(_ => {
                var axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                transform.Rotate(0, axis * (_rotational_speed * Time.deltaTime) * ADD_FORCE_VALUE, 0);
            });

            /// <summary>
            /// pitch.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_up_button.isPressed || _down_button.isPressed)).Subscribe(_ => {
                var axis = _up_button.isPressed ? 1 : _down_button.isPressed ? -1 : 0;
                transform.Rotate(axis * (_pitch_speed * Time.deltaTime) * ADD_FORCE_VALUE, 0, 0);

                Debug.Log($"_down_button.isPressed: {_down_button.isPressed}");
            });

            /// <summary>
            /// roll and yaw.
            /// </summary>
            if (_game_system.mode == Envelope.MODE_EASY || _game_system.mode == Envelope.MODE_NORMAL) {
                this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_button.isPressed || _right_button.isPressed)).Subscribe(_ => {
                    var axis = _left_button.isPressed ? 1 : _right_button.isPressed ? -1 : 0;
                    transform.Rotate(0, 0, axis * (_roll_speed * Time.deltaTime) * ADD_FORCE_VALUE);
                    axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                    transform.Rotate(0, axis * (_roll_speed * Time.deltaTime) * ADD_FORCE_VALUE, 0);
                });
            } 
            else if (_game_system.mode == Envelope.MODE_HARD) {
                this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_button.isPressed || _right_button.isPressed)).Subscribe(_ => {
                    var axis = _left_button.isPressed ? 1 : _right_button.isPressed ? -1 : 0;
                    transform.Rotate(0, 0, axis * (_roll_speed * 2.0f * Time.deltaTime) * ADD_FORCE_VALUE);
                });
                this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_1_button.isPressed || _right_1_button.isPressed)).Subscribe(_ => {
                    var axis = _right_1_button.isPressed ? 1 : _left_1_button.isPressed ? -1 : 0;
                    transform.Rotate(0, axis * (_roll_speed * 0.5f * Time.deltaTime) * ADD_FORCE_VALUE, 0);
                });
            }
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && _do_update.banking).Subscribe(_ => {
                float angle = bank > 90 ? 90 - (bank - 90) : bank;
                float power = (float) (angle * (airSpeed / 2) * 0.001f);
                // yaw against the world.
                var axis = roll >= 180 ? 1 : roll < 180 ? -1 : 0;
                transform.Rotate(new Vector3(0, axis * (_roll_speed * Time.deltaTime) * power, 0), Space.World);
            });

            /// <summary>
            /// left quick roll.
            /// </summary>
            const float WAIT_FOR_DOUBLE_CLICK = 250f;
            float left_quick_roll_time_count = 0f;
            Vector3 left_quick_roll_angle = new(0f, 0f, 0f);
            float left_quick_roll_to_z = 0f;
            var left_double_click = this.UpdateAsObservable().Where(_ => _left_button.wasPressedThisFrame);
            left_double_click.Buffer(left_double_click.Throttle(TimeSpan.FromMilliseconds(WAIT_FOR_DOUBLE_CLICK))).Where(x => x.Count == 2).Subscribe(_ => {
                _do_update.needLeftQuickRoll = true;
                left_quick_roll_time_count = 0f;
                left_quick_roll_angle = transform.rotation.eulerAngles;
                left_quick_roll_to_z = roll >= 180 ? left_quick_roll_angle.z - 180f : -(left_quick_roll_angle.z - 180f);
            });
            this.UpdateAsObservable().Where(_ => _do_update.needLeftQuickRoll).Subscribe(_ => {
                left_quick_roll_angle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, left_quick_roll_to_z);
                Quaternion quick_roll_rotation = Quaternion.Euler(left_quick_roll_angle);
                left_quick_roll_time_count += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, quick_roll_rotation, left_quick_roll_time_count);
                if (left_quick_roll_time_count >= 1) { _do_update.needLeftQuickRoll = false; }
            });

            /// <summary>
            /// right quick roll.
            /// </summary>
            float right_quick_roll_time_count = 0f;
            Vector3 right_quick_roll_angle = new(0f, 0f, 0f);
            float right_quick_roll_to_z = 0f;
            var right_double_click = this.UpdateAsObservable().Where(_ => _right_button.wasPressedThisFrame);
            right_double_click.Buffer(right_double_click.Throttle(TimeSpan.FromMilliseconds(WAIT_FOR_DOUBLE_CLICK))).Where(x => x.Count == 2).Subscribe(_ => {
                _do_update.needRightQuickRoll = true;
                right_quick_roll_time_count = 0f;
                right_quick_roll_angle = transform.rotation.eulerAngles;
                right_quick_roll_to_z = roll < 180 ? right_quick_roll_angle.z - 180f : -(right_quick_roll_angle.z - 180f);
            });
            this.UpdateAsObservable().Where(_ => _do_update.needRightQuickRoll).Subscribe(_ => {
                right_quick_roll_angle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, right_quick_roll_to_z);
                Quaternion quick_roll_rotation = Quaternion.Euler(right_quick_roll_angle);
                right_quick_roll_time_count += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, quick_roll_rotation, right_quick_roll_time_count);
                if (right_quick_roll_time_count >= 1) { _do_update.needRightQuickRoll = false; }
            });

            /// <summary>
            /// stall.
            /// </summary>
            float stall_time_count = 0f;
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && _energy.power < 1.0f && flightTime > 0.5f).Subscribe(_ => {
                Debug.Log($"stall");
                _do_update.stall = true;
                stall_time_count = 0f;
                OnStall?.Invoke();
            });
            this.UpdateAsObservable().Where(_ => _do_update.stall).Subscribe(_ => {
                var ground_object = Find(Envelope.GROUND_TYPE);
                Quaternion ground_rotation = Quaternion.LookRotation(ground_object.transform.position);
                stall_time_count += Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, ground_rotation, stall_time_count);
                if (_energy.power < 10.0f || _energy.total < 10.0f || _energy.speed < 10.0f) { _energy.Gain(); }
                if (stall_time_count >= 1) { _do_update.stall = false; }
            });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(_ => {
                position = transform.position;
                rotation = transform.rotation;
                flightTime = (float) _flight_stopwatch.Elapsed.TotalSeconds;
                if (bank > 1.0f) { // FIXME:
                    _do_update.banking = true;
                } else {
                    _do_update.banking = false;
                }
                _energy.pitch = pitch;
            });

            /// <summary>
            /// when touching grounds.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(x => x.LikeGround()).Subscribe(x => {
                _do_update.grounded = true;
                rb.useGravity = true;

                // reset rotate.
                Vector3 angle = transform.eulerAngles;
                angle.x = angle.z = 0f;
                transform.eulerAngles = angle;

                useLiftSpoiler = false; // reset lift spoiler.
                _energy.hasLanded = true; // reset flight power.
                _flight_stopwatch.Reset(); // reset flight time.
                OnGrounded?.Invoke(); // call event handler.
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// the value until the top of the block.
        /// </summary>
        double getReach() {
            return Math.Round(transform.position.y, 2) % 1; // FIXME:
        }

        /// <summary>
        /// move top when the vehicle hits a block.
        /// </summary>
        void moveTop() {
            const float MOVE_VALUE = 6.0f;
            transform.position = new(
                transform.position.x,
                transform.position.y + MOVE_VALUE * Time.deltaTime,
                transform.position.z
            );
        }

        /// <summary>
        /// move aside when the vehicle hits a block.
        /// </summary>
        /// <param name="direction">the vehicle's direction is provided.</param>
        void moveLetfOrRight(Direction direction) {
            const float MOVE_VALUE = 0.3f;
            Vector3 move_position = transform.position;
            // z-axis positive and negative.
            if (direction == Direction.PositiveZ || direction == Direction.NegativeZ) {
                if (transform.forward.x < 0f) {
                    move_position = new(
                        transform.position.x - MOVE_VALUE * Time.deltaTime,
                        transform.position.y,
                        transform.position.z
                    );
                }
                else if (transform.forward.x >= 0f) {
                    move_position = new(
                        transform.position.x + MOVE_VALUE * Time.deltaTime,
                        transform.position.y,
                        transform.position.z
                    );
                }
            }
            // x-axis positive and negative.
            if (direction == Direction.PositiveX || direction == Direction.NegativeX) {
                if (transform.forward.z < 0f) {
                    move_position = new(
                        transform.position.x,
                        transform.position.y,
                        transform.position.z - MOVE_VALUE * Time.deltaTime
                    );
                }
                else if (transform.forward.z >= 0f) {
                    move_position = new(
                        transform.position.x,
                        transform.position.y,
                        transform.position.z + MOVE_VALUE * Time.deltaTime
                    );
                }
            }
            // move to a new position.
            transform.position = move_position;
        }

        /// <summary>
        /// returns an enum of the vehicle's direction.
        /// </summary>
        Direction getDirection(Vector3 forwardVector) {
            var forward_x = (float) Math.Round(forwardVector.x);
            var forward_y = (float) Math.Round(forwardVector.y);
            var forward_z = (float) Math.Round(forwardVector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // x-axis positive.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // x-axis negative.
            // determine the difference between the two axes.
            float absolute_x = Math.Abs(forwardVector.x);
            float absolute_z = Math.Abs(forwardVector.z);
            if (absolute_x > absolute_z) {
                if (forward_x == 1) { return Direction.PositiveX; } // x-axis positive.
                if (forward_x == -1) { return Direction.NegativeX; } // x-axis negative.
            }
            else if (absolute_x < absolute_z) {
                if (forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
                if (forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            }
            return Direction.None; // unknown.
        }

        /// <summary>
        /// whether hits the side of the colliding object.
        /// </summary>
        bool isHitSide(GameObject target) {
            const float ADJUST = 0.1f;
            float target_height = target.GetRenderer().bounds.size.y;
            float target_y = target.transform.position.y;
            float target_top = target_height + target_y;
            var position_y = transform.position.y;
            if (position_y < (target_top - ADJUST)) {
                return true;
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// changed event handler from energy.
        /// </summary>
        void onChanged(object sender, EvtArgs  e) {
            if (sender as Energy is not null) {
                if (e.Name.Equals(nameof(Energy.total))) {
                    total = _energy.total;
                }
                if (e.Name.Equals(nameof(Energy.power))) {
                    power = _energy.power;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        #region DoUpdate

        /// <summary>
        /// class for the Update() method.
        /// </summary>
        class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _grounded, _stall, _banking, _need_left_quick_roll, _need_right_quick_roll;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool grounded {
                get => _grounded;
                set => _grounded = value;
            }

            public bool stall {
                get => _stall;
                set => _stall = value;
            }

            public bool banking {
                get => _banking;
                set => _banking = value;
            }

            public bool needLeftQuickRoll {
                get => _need_left_quick_roll;
                set => _need_left_quick_roll = value;
            }

            public bool needRightQuickRoll {
                get => _need_right_quick_roll;
                set => _need_right_quick_roll = value;
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static DoUpdate GetInstance() {
                var instance = new DoUpdate();
                instance.ResetState();
                return instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public void ResetState() {
                _grounded = _stall = _banking = _need_left_quick_roll = _need_right_quick_roll = false;
            }
        }

        #endregion

        #region DoFixedUpdate

        /// <summary>
        /// class for the FixedUpdate() method.
        /// </summary>
        class DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _idol, _run, _walk, _jump, _backward, _flight;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool idol { get => _idol; }
            public bool run { get => _run; }
            public bool walk { get => _walk; }
            public bool jump { get => _jump; }
            public bool backward { get => _backward; }
            public bool flight { get => _flight; }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static DoFixedUpdate GetInstance() {
                return new DoFixedUpdate();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public void ApplyIdol() {
                _idol = true;
                _run = _walk = _backward = _jump = _flight = false;
            }

            public void ApplyRun() {
                _idol = _walk = _backward = _flight = false;
                _run = true;
            }

            public void CancelRun() {
                _run = false;
            }

            public void ApplyWalk() {
                _idol = _run = _backward = _flight = false;
                _walk = true;
            }

            public void CancelWalk() {
                _walk = false;
            }

            public void ApplyBackward() {
                _idol = _run = _walk = _flight = false;
                _backward = true;
            }

            public void CancelBackward() {
                _backward = false;
            }

            public void ApplyJump() {
                _jump = true;
            }

            public void CancelJump() {
                _jump = false;
            }

            public void ApplyFlight() {
                _idol = _walk = _run = _backward = false;
                _flight = true;
            }

            public void CancelFlight() {
                _flight = false;
            }
        }

        #endregion

        #region Acceleration

        class Acceleration {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            float _current_speed;
            float _previous_speed;
            float _forward_speed_limit;
            float _run_speed_limit;
            float _backward_speed_limit;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float currentSpeed { get => _current_speed; set => _current_speed = value; }
            public float previousSpeed { get => _previous_speed; set => _previous_speed = value; }
            public bool canWalk { get => _current_speed < _forward_speed_limit; }
            public bool canRun { get => _current_speed < _run_speed_limit; }
            public bool canBackward { get => _current_speed < _backward_speed_limit; }
            public bool freeze {
                get {
                    if (Math.Round(_previous_speed, 2) < 0.02 &&
                        Math.Round(_current_speed, 2) < 0.02 &&
                        Math.Round(_previous_speed, 2) == Math.Round(_current_speed, 2)) {
                        return true;
                    }
                    return false;
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// hide the constructor.
            /// </summary>
            Acceleration(float forwardSpeedLimit, float runSpeedLimit, float backwardSpeedLimit) {
                _forward_speed_limit = forwardSpeedLimit;
                _run_speed_limit = runSpeedLimit;
                _backward_speed_limit = backwardSpeedLimit;
            }

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static Acceleration GetInstance(float forwardSpeedLimit, float runSpeedLimit, float backwardSpeedLimit) {
                return new Acceleration(forwardSpeedLimit, runSpeedLimit, backwardSpeedLimit);
            }
        }

        #endregion

        #region Energy

        class Energy {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            float _flight_power_base, _default_flight_power_base, _calculated_power;

            float _altitude;

            Queue<float> _previous_altitudes = new();

            float _speed;

            float _threshold = 1f; // FIXME:

            float _pitch;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            /// <summary>
            /// altitude.
            /// </summary>
            public float altitude {
                set {
                    const int QUEUE_COUNT = Envelope.FPS / 2; // 0.5 sec.
                    if (_previous_altitudes.Count < QUEUE_COUNT) {
                        _previous_altitudes.Enqueue(_altitude);
                    }
                    else {
                        _previous_altitudes.Dequeue(); // keep the queue count.
                        _previous_altitudes.Enqueue(_altitude);
                    }
                    _altitude = value; Updated?.Invoke(this, new(nameof(total)));
                }
            }

            /// <summary>
            /// speed in flighting.
            /// </summary>
            public float speed {
                get => _speed;
                set { _speed = value; Updated?.Invoke(this, new(nameof(total))); }
            }

            /// <summary>
            /// total energy.
            /// </summary>
            public float total {
                get => _altitude + _speed;
            }

            /// <summary>
            /// power for velocity.
            /// </summary>
            public float power {
                get {
                    return _calculated_power;
                }
            }

            /// <summary>
            /// whether it has landed.
            /// </summary>
            public bool hasLanded {
                set {
                    if (value) {
                        _speed = 0;
                        _flight_power_base = _default_flight_power_base;
                    }
                }
            }

            /// <summary>
            /// pitch
            /// </summary>
            public float pitch {
                set => _pitch = value;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            //internal  Events [verb, verb phrase] 

            /// <summary>
            /// changed event handler.
            /// </summary>
            internal event Changed? Updated;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// hide the constructor.
            /// </summary>
            Energy(float flightPower) {
                _flight_power_base = flightPower;
                _default_flight_power_base = _flight_power_base;
            }

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static Energy GetInstance(float flightPower) {
                return new Energy(flightPower);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            /// <summary>
            /// get the calculated power.
            /// </summary>
            public float GetCalculatedPower() {
                const float ADD_OR_SUBTRACT_VALUE = 0.0009375f;
                const float POWAR_FACTOR_VALUE = 5.0f;
                const float ADJUSTED_VALUE = 2.65f;
                const float AUTO_FLARE_ALTITUDE = 8.0f;
                if (total > _threshold) {
                    if (_previous_altitudes.Peek() < _altitude) { // up
                        float pitch_factor = 1.0f;
                        pitch_factor += Math.Abs(_pitch / 100f);
                        _flight_power_base -= ADD_OR_SUBTRACT_VALUE * POWAR_FACTOR_VALUE * _total_power_factor_value * pitch_factor;
                    }
                    if (_previous_altitudes.Peek() > _altitude) { // down
                        float pitch_factor = 1.0f;
                        pitch_factor += Math.Abs(_pitch / 100f);
                        _flight_power_base += ADD_OR_SUBTRACT_VALUE * POWAR_FACTOR_VALUE * _total_power_factor_value * pitch_factor;
                    }
                }
                if (total <= _threshold && _altitude < AUTO_FLARE_ALTITUDE) {
                    _flight_power_base = _default_flight_power_base;
                }
                float power_value = _flight_power_base * ADJUSTED_VALUE * _total_power_factor_value;
                _calculated_power = power_value < 0 ? 0 : power_value;
                Updated?.Invoke(this, new(nameof(power))); // call event handler.
                return _calculated_power;
            }

            /// <summary>
            /// gain the power.
            /// </summary>
            public void Gain() {
                const float ADD_VALUE = 0.125f;
                _flight_power_base += ADD_VALUE * _total_power_factor_value; ;
            }

            /// <summary>
            /// lose the power.
            /// </summary>
            public void Lose() {
                const float SUBTRACT_VALUE = 0.125f;
                _flight_power_base -= SUBTRACT_VALUE * _total_power_factor_value; ;
            }
        }

        #endregion
    }
}
