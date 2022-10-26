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
using static System.Math;
using UnityEngine;
using static UnityEngine.GameObject;
using static UnityEngine.Quaternion;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

using static Studio.MeowToon.Env;

namespace Studio.MeowToon {
    /// <summary>
    /// vehicle controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Vehicle : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] float _jump_power = 15.0f;

        [SerializeField] float _rotational_speed = 10.0f;

        [SerializeField] float _pitch_speed = 5.0f;

        [SerializeField] float _roll_speed = 5.0f;

        [SerializeField] float _flight_power = 5.0f;

        [SerializeField] float _forward_speed_limit = 1.1f;

        [SerializeField] float _run_speed_limit = 3.25f;

        [SerializeField] float _backward_speed_limit = 0.75f;

        [SerializeField] SimpleAnimation _simple_anime;

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region static Fields [noun, adjectives] 

        static float _total_power_factor_value = 1.5f;

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region Fields [noun, adjectives] 

        GameSystem _game_system;

        DoUpdate _do_update;

        DoFixedUpdate _do_fixed_update;

        Acceleration _acceleration;

        Energy _energy;

        System.Diagnostics.Stopwatch _flight_stopwatch = new();

        float _flight_time, _air_speed, _vertical_speed, _total, _power = 0f;

        bool _use_lift_spoiler = false;

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region Properties [noun, adjectives] 

        /// <summary>
        /// current speed of the vehicle object to flight.
        /// </summary>
        public float airSpeed { get => _air_speed; set { _air_speed = value; Updated?.Invoke(this, new(nameof(airSpeed))); }}

        /// current vertical speed of the vehicle object to flight.
        /// </summary>
        public float verticalSpeed { get => _vertical_speed; set { _vertical_speed = value; Updated?.Invoke(this, new(nameof(verticalSpeed))); }}

        /// <summary>
        /// elapsed time after takeoff.
        /// </summary>
        public float flightTime { get => _flight_time; set { _flight_time = value; Updated?.Invoke(this, new(nameof(flightTime))); }}

        /// <summary>
        /// total energy.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float total { get => _total; set { _total = value; Updated?.Invoke(this, new(nameof(total))); }}

        /// <summary>
        /// current power of the vehicle to flight.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float power { get => _power; set { _power = value; Updated?.Invoke(this, new(nameof(power))); }}

        /// <summary>
        /// use or not use lift spoiler.
        /// </summary>
        public bool useLiftSpoiler { get => _use_lift_spoiler; set { _use_lift_spoiler = value; Updated?.Invoke(this, new(nameof(useLiftSpoiler))); }}

        /// <summary>
        /// transform position.
        /// </summary>
        public Vector3 position { get => transform.position; set { transform.position = value; Updated?.Invoke(this, new(nameof(position))); }}

        /// <summary>
        /// transform rotation.
        /// </summary>
        public Quaternion rotation { get => transform.rotation; set { transform.rotation = value; Updated?.Invoke(this, new(nameof(rotation))); }}

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

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region public Events [verb, verb phrase]

        public event Action? OnFlight;

        public event Action? OnGrounded;

        public event Action? OnGainEnergy;

        public event Action? OnLoseEnergy;

        public event Action? OnStall;

        /// <summary>
        /// changed event handler.
        /// </summary>
        public event Changed? Updated;

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
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
                case MODE_EASY: _total_power_factor_value = 1.0f; break;
                case MODE_NORMAL: _total_power_factor_value = 1.5f; break;
                case MODE_HARD: _total_power_factor_value = 2.0f; break;
            }

            /// <remarks>
            /// Rigidbody should be only used in FixedUpdate.
            /// </remarks>
            Rigidbody rb = transform.Get<Rigidbody>();

            // FIXME: to integrate with Energy function.
            this.FixedUpdateAsObservable().Subscribe(onNext: _ => {
                _acceleration.previousSpeed = _acceleration.currentSpeed;// hold previous speed.
                _acceleration.currentSpeed = rb.velocity.magnitude; // get speed.
            }).AddTo(this);

            // get vehicle speed for flight.
            Vector3 prev_position = transform.position;
            this.FixedUpdateAsObservable().Where(predicate: _ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(onNext: _ => {
                const float VALUE_MS_TO_KMH = 3.6f; // m/s -> km/h
                airSpeed = ((transform.position - prev_position) / Time.deltaTime).magnitude * VALUE_MS_TO_KMH; 
                verticalSpeed = ((transform.position.y - prev_position.y) / Time.deltaTime); // m/s
                prev_position = transform.position;
            }).AddTo(this);

            #region for Grounded Player

            /// <summary>
            /// idol.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.grounded && !_up_button.isPressed && !_down_button.isPressed).Subscribe(onNext: _ => {
                //_simpleAnime.Play("Default");
                _do_fixed_update.ApplyIdol();
            }).AddTo(this);

            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.idol).Subscribe(onNext: _ => {
                rb.useGravity = true;
            }).AddTo(this);

            /// <summary>
            /// walk.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.grounded && _up_button.isPressed && !_y_button.isPressed).Subscribe(onNext: _ => {
                /*_simpleAnime.Play("Walk");*/
                _do_fixed_update.ApplyWalk();
            }).AddTo(this);

            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.walk && _acceleration.canWalk).Subscribe(onNext: _ => {
                const float ADJUST_VALUE = 7.5f;
                rb.AddFor​​ce(force: transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                _do_fixed_update.CancelWalk();
            }).AddTo(this);

            /// <summary>
            /// run.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.grounded && _up_button.isPressed && _y_button.isPressed).Subscribe(onNext: _ => {
                /*_simpleAnime.Play("Run");*/
                _do_fixed_update.ApplyRun();
            }).AddTo(this);

            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.run && _acceleration.canRun).Subscribe(onNext: _ => {
                const float ADJUST_VALUE = 7.5f;
                rb.AddFor​​ce(force: transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                _do_fixed_update.CancelRun();
            }).AddTo(this);

            /// <summary>
            /// backward.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.grounded && _down_button.isPressed).Subscribe(onNext: _ => {
                /*_simpleAnime.Play("Walk");*/
                _do_fixed_update.ApplyBackward();
            }).AddTo(this);

            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.backward && _acceleration.canBackward).Subscribe(onNext: _ => {
                const float ADJUST_VALUE = 7.5f;
                rb.AddFor​​ce(force: -transform.forward * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                _do_fixed_update.CancelBackward();
            }).AddTo(this);

            /// <summary>
            /// jump.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _b_button.wasPressedThisFrame && _do_update.grounded).Subscribe(onNext: _ => {
                _do_update.grounded = false;
                //_simpleAnime.Play("Jump");
                _do_fixed_update.ApplyJump();
            }).AddTo(this);

            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.jump).Subscribe(onNext: _ => {
                const float ADJUST_VALUE = 2.0f;
                rb.useGravity = true;
                rb.AddRelativeFor​​ce(force: up * _jump_power * ADD_FORCE_VALUE * ADJUST_VALUE, mode: ForceMode.Acceleration);
                _do_fixed_update.CancelJump();
            }).AddTo(this);

            /// <summary>
            /// rotate(yaw).
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.grounded).Subscribe(onNext: _ => {
                int axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                transform.Rotate(xAngle: 0, yAngle: axis * (_rotational_speed * Time.deltaTime) * ADD_FORCE_VALUE, zAngle: 0);
            }).AddTo(this);

            /// <summary>
            /// freeze.
            /// </summary>
            this.OnCollisionStayAsObservable().Where(predicate: x => x.Like(BLOCK_TYPE) && (_up_button.isPressed || _down_button.isPressed) && _acceleration.freeze).Subscribe(onNext: _ => {
                double reach = getReach();
                if (_do_update.grounded && (reach < 0.5d || reach >= 0.99d)) {
                    moveLetfOrRight(direction: getDirection(forward_vector: transform.forward));
                }
                else if (reach >= 0.5d && reach < 0.99d) {
                    rb.useGravity = false;
                    moveTop();
                }
            }).AddTo(this);

            /// <summary>
            /// when touching blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(predicate: x => x.Like(BLOCK_TYPE)).Subscribe(onNext: x => {
                if (!isHitSide(target: x.gameObject)) {
                    _do_update.grounded = true;
                    rb.useGravity = true;
                }
            }).AddTo(this);

            /// <summary>
            /// when leaving blocks.
            /// TODO: to Block ?
            /// </summary>
            this.OnCollisionExitAsObservable().Where(predicate: x => x.Like(BLOCK_TYPE)).Subscribe(onNext: x => {
                rb.useGravity = true;
            }).AddTo(this);

            #endregion

            # region for Flight Vehicle

            /// <summary>
            /// flight.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting).Subscribe(onNext: _ => {
                const float VEHICLE_HEIGHT_OFFSET = 0.0f;
                _energy.speed = airSpeed;
                _energy.altitude = transform.position.y - VEHICLE_HEIGHT_OFFSET;
                _energy.hasLanded = false;
                _do_fixed_update.ApplyFlight();
                OnFlight?.Invoke(); // call event handler.
            }).AddTo(this);
            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.flight).Subscribe(onNext: _ => {
                const float FALL_VALUE = 5.0f; // 5.0 -> -5m/s
                rb.useGravity = false;
                rb.velocity = transform.forward * _energy.GetCalculatedPower();
                if (useLiftSpoiler) {
                    Vector3 velocity = rb.velocity;
                    rb.velocity = new Vector3(x: velocity.x, y: velocity.y - FALL_VALUE, z: velocity.z);
                }
                _do_fixed_update.CancelFlight();
                _flight_stopwatch.Start();
            }).AddTo(this);

            #region Pitch

            /// <summary>
            /// pitch.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && (_up_button.isPressed || _down_button.isPressed)).Subscribe(onNext: _ => {
                int pitch_axis = _up_button.isPressed ? 1 : _down_button.isPressed ? -1 : 0;
                transform.Rotate(xAngle: pitch_axis * _pitch_speed * _energy.ratio * Time.deltaTime * ADD_FORCE_VALUE, yAngle: 0, zAngle: 0);
            }).AddTo(this);

            #endregion

            #region Roll and Yaw

            /// <summary>
            /// roll and yaw.
            /// </summary>
            if (_game_system.mode == MODE_EASY) {
                this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && (_left_button.isPressed || _right_button.isPressed)).Subscribe(onNext: _ => {
                    int roll_axis = _left_button.isPressed ? 1 : _right_button.isPressed ? -1 : 0;
                    transform.Rotate(xAngle: 0, yAngle: 0, zAngle: roll_axis * _roll_speed * _energy.ratio * Time.deltaTime * ADD_FORCE_VALUE);
                    int yaw_axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                    transform.Rotate(xAngle: 0, yAngle: yaw_axis * _roll_speed * _energy.ratio * Time.deltaTime * ADD_FORCE_VALUE, zAngle: 0);
                }).AddTo(this);
            } else if (_game_system.mode == MODE_NORMAL || _game_system.mode == MODE_HARD) {
                this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && (_left_button.isPressed || _right_button.isPressed)).Subscribe(onNext: _ => {
                    int roll_axis = _left_button.isPressed ? 1 : _right_button.isPressed ? -1 : 0;
                    transform.Rotate(xAngle: 0, yAngle: 0, zAngle: roll_axis * _roll_speed * _energy.ratio * 2.0f * Time.deltaTime * ADD_FORCE_VALUE);
                }).AddTo(this);
                this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && (_left_1_button.isPressed || _right_1_button.isPressed)).Subscribe(onNext: _ => {
                    int yaw_axis = _right_1_button.isPressed ? 1 : _left_1_button.isPressed ? -1 : 0;
                    transform.Rotate(xAngle: 0, yAngle: yaw_axis * _roll_speed * _energy.ratio * 0.5f * Time.deltaTime * ADD_FORCE_VALUE, zAngle: 0);
                }).AddTo(this);
            }
            /// <summary>
            /// persistent yaw against the world.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _do_update.banking).Subscribe(onNext: _ => {
                const float ADJUSTED_VALUE = 0.001f;
                float angle = bank > 90 ? 90 - (bank - 90) : bank;
                float power = (float) (angle * (airSpeed / 2) * ADJUSTED_VALUE);
                int yaw_axis = roll >= 180 ? 1 : roll < 180 ? -1 : 0;
                transform.Rotate(eulers: new Vector3(x: 0, y: yaw_axis * _roll_speed * _energy.ratio * Time.deltaTime * power, z: 0), relativeTo: Space.World);
            }).AddTo(this);

            #endregion

            #region Quick Roll

            /// <summary>
            /// left quick roll.
            /// </summary>
            const float WAIT_FOR_DOUBLE_CLICK = 250f;
            float left_quick_roll_time_count = 0f;
            Vector3 left_quick_roll_angle = new(0f, 0f, 0f);
            float left_quick_roll_to_z = 0f;
            IObservable<Unit> left_double_click = this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _left_button.wasPressedThisFrame && _a_button.isPressed);
            left_double_click.Buffer(left_double_click.Throttle(TimeSpan.FromMilliseconds(WAIT_FOR_DOUBLE_CLICK))).Where(x => x.Count == 2).Subscribe(onNext: _ => {
                _do_update.needLeftQuickRoll = true;
                left_quick_roll_time_count = 0f;
                left_quick_roll_angle = transform.rotation.eulerAngles;
                left_quick_roll_to_z = roll >= 180 ? left_quick_roll_angle.z - 180f : -(left_quick_roll_angle.z - 180f);
            });
            this.UpdateAsObservable().Where(predicate: _ => _do_update.needLeftQuickRoll).Subscribe(onNext: _ => {
                left_quick_roll_angle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, left_quick_roll_to_z);
                Quaternion quick_roll_rotation = Euler(euler: left_quick_roll_angle);
                left_quick_roll_time_count += Time.deltaTime * _energy.ratio;
                transform.rotation = Slerp(a: transform.rotation, b: quick_roll_rotation, t: left_quick_roll_time_count);
                if (left_quick_roll_time_count >= 1) { _do_update.needLeftQuickRoll = false; }
            }).AddTo(this);

            /// <summary>
            /// right quick roll.
            /// </summary>
            float right_quick_roll_time_count = 0f;
            Vector3 right_quick_roll_angle = new(0f, 0f, 0f);
            float right_quick_roll_to_z = 0f;
            IObservable<Unit> right_double_click = this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _right_button.wasPressedThisFrame && _a_button.isPressed);
            right_double_click.Buffer(right_double_click.Throttle(TimeSpan.FromMilliseconds(WAIT_FOR_DOUBLE_CLICK))).Where(x => x.Count == 2).Subscribe(onNext: _ => {
                _do_update.needRightQuickRoll = true;
                right_quick_roll_time_count = 0f;
                right_quick_roll_angle = transform.rotation.eulerAngles;
                right_quick_roll_to_z = roll < 180 ? right_quick_roll_angle.z - 180f : -(right_quick_roll_angle.z - 180f);
            }).AddTo(this);
            this.UpdateAsObservable().Where(predicate: _ => _do_update.needRightQuickRoll).Subscribe(onNext: _ => {
                right_quick_roll_angle = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, right_quick_roll_to_z);
                Quaternion quick_roll_rotation = Euler(euler: right_quick_roll_angle);
                right_quick_roll_time_count += Time.deltaTime * _energy.ratio;
                transform.rotation = Slerp(a: transform.rotation, b: quick_roll_rotation, t: right_quick_roll_time_count);
                if (right_quick_roll_time_count >= 1) { _do_update.needRightQuickRoll = false; }
            }).AddTo(this);

            # endregion

            /// <summary>
            /// stall.
            /// </summary>
            float stall_time_count = 0f;
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _energy.power < 1.0f && flightTime > 0.5f).Subscribe(onNext: _ => {
                //Debug.Log($"stall");
                _do_update.stalling = true;
                stall_time_count = 0f;
                OnStall?.Invoke();
            }).AddTo(this);
            this.UpdateAsObservable().Where(predicate: _ => _do_update.stalling).Subscribe(onNext: _ => {
                GameObject ground_object = Find(name: GROUND_TYPE);
                Quaternion ground_rotation = LookRotation(forward: ground_object.transform.position);
                stall_time_count += Time.deltaTime;
                transform.rotation = Slerp(a: transform.rotation, b: ground_rotation, t: stall_time_count);
                if (_energy.power < 10.0f || _energy.total < 10.0f || _energy.speed < 10.0f) { _energy.Gain(); } // FIXME: into _energy
                if (stall_time_count >= 1) { _do_update.stalling = false; }
            }).AddTo(this);

            #region Gain or Lose Energy

            /// <summary>
            /// gain energy.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _b_button.wasPressedThisFrame && _game_system.usePoint).Subscribe(onNext: _ => {
                _energy.Gain();
                OnGainEnergy?.Invoke();
            }).AddTo(this);

            /// <summary>
            /// lose energy.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _y_button.wasPressedThisFrame && _game_system.usePoint).Subscribe(onNext: _ => {
                _energy.Lose();
                OnLoseEnergy?.Invoke();
            }).AddTo(this);

            # endregion

            /// <summary>
            /// use or not use lift spoiler.
            /// </summary>
            this.UpdateAsObservable().Where(predicate: _ => _do_update.flighting && _x_button.wasPressedThisFrame).Subscribe(onNext: _ => {
                useLiftSpoiler = !useLiftSpoiler;
            }).AddTo(this);

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(onNext: _ => {
                position = transform.position;
                rotation = transform.rotation;
                flightTime = (float) _flight_stopwatch.Elapsed.TotalSeconds;
                if (bank > 1.0f) { // FIXME:
                    _do_update.banking = true;
                } else {
                    _do_update.banking = false;
                }
                _energy.pitch = pitch;
            }).AddTo(this);

            /// <summary>
            /// when touching grounds.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(predicate: x => x.Like(GROUND_TYPE)).Subscribe(onNext: x => {
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
            }).AddTo(this);
        }

        #endregion

        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region private Methods [verb]

        /// <summary>
        /// the value until the top of the block.
        /// </summary>
        double getReach() {
            return Round(value: transform.position.y, digits: 2) % 1; // FIXME:
        }

        /// <summary>
        /// move top when the vehicle hits a block.
        /// </summary>
        void moveTop() {
            const float MOVE_VALUE = 6.0f;
            transform.position = new(
                x: transform.position.x,
                y: transform.position.y + MOVE_VALUE * Time.deltaTime,
                z: transform.position.z
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
                        x: transform.position.x - MOVE_VALUE * Time.deltaTime,
                        y: transform.position.y,
                        z: transform.position.z
                    );
                } else if (transform.forward.x >= 0f) {
                    move_position = new(
                        x: transform.position.x + MOVE_VALUE * Time.deltaTime,
                        y: transform.position.y,
                        z: transform.position.z
                    );
                }
            }
            // x-axis positive and negative.
            if (direction == Direction.PositiveX || direction == Direction.NegativeX) {
                if (transform.forward.z < 0f) {
                    move_position = new(
                        x: transform.position.x,
                        y: transform.position.y,
                        z: transform.position.z - MOVE_VALUE * Time.deltaTime
                    );
                } else if (transform.forward.z >= 0f) {
                    move_position = new(
                        x: transform.position.x,
                        y: transform.position.y,
                        z: transform.position.z + MOVE_VALUE * Time.deltaTime
                    );
                }
            }
            // move to a new position.
            transform.position = move_position;
        }

        /// <summary>
        /// returns an enum of the vehicle's direction.
        /// </summary>
        Direction getDirection(Vector3 forward_vector) {
            float forward_x = (float) Round(a: forward_vector.x);
            float forward_y = (float) Round(a: forward_vector.y);
            float forward_z = (float) Round(a: forward_vector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // x-axis positive.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // x-axis negative.
            // determine the difference between the two axes.
            float absolute_x = Abs(value: forward_vector.x);
            float absolute_z = Abs(value: forward_vector.z);
            if (absolute_x > absolute_z) {
                if (forward_x == 1) { return Direction.PositiveX; } // x-axis positive.
                if (forward_x == -1) { return Direction.NegativeX; } // x-axis negative.
            } else if (absolute_x < absolute_z) {
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
            float target_height = target.Get<Renderer>().bounds.size.y;
            float target_y = target.transform.position.y;
            float target_top = target_height + target_y;
            float position_y = transform.position.y;
            if (position_y < (target_top - ADJUST)) {
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// changed event handler from energy.
        /// </summary>
        void onChanged(object sender, EvtArgs  e) {
            if (sender as Energy is not null) {
                if (e.Name.Equals(value: nameof(Energy.total))) { total = _energy.total; }
                if (e.Name.Equals(value: nameof(Energy.power))) { power = _energy.power; }
            }
        }

        #endregion
    }
}
