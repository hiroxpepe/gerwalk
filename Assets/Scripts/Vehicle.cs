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
using System.ComponentModel;
using UnityEngine;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// vehicle controller.
    /// @author h.adachi
    /// </summary>
    public class Vehicle : GamepadMaper {
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
        // Fields [noun, adjectives] 

        StatusSystem _status_system;

        DoUpdate _do_update;

        DoFixedUpdate _do_fixed_update;

        Acceleration _acceleration;

        Energy _energy;

        System.Diagnostics.Stopwatch _flight_stopwatch = new();

        float _air_speed = 0f;

        float _vertical_speed = 0f;

        bool _use_lift_spoiler = false;

        Action _onFlight = () => { };

        Action _onGrounded = () => { };

        Action _onGainEnergy = () => { };

        Action _onLoseEnergy = () => { };

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// current speed of the vehicle object to flight.
        /// </summary>
        public float airSpeed {
            get => _air_speed;
            set {
                _air_speed = value;
                Updated?.Invoke(this, new(nameof(airSpeed)));
            }
        }

        /// current vertical speed of the vehicle object to flight.
        /// </summary>
        public float verticalSpeed { 
            get => _vertical_speed;
            set {
                _vertical_speed = value;
                Updated?.Invoke(this, new(nameof(verticalSpeed)));
            }
        }

        /// <summary>
        /// elapsed time after takeoff.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float flightTime { get => (float)_flight_stopwatch.Elapsed.TotalSeconds; }

        /// <summary>
        /// current power of the vehicle object to flight.
        /// </summary>
        public float power { get => _energy.power; }

        /// <summary>
        /// total energy.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float total { get => _energy.total; }

        /// <summary>
        /// use or not use lift spoiler.
        /// </summary>
        public bool useLiftSpoiler { get => _use_lift_spoiler; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action OnFlight { add => _onFlight += value; remove => _onFlight -= value; }

        public event Action OnGrounded { add => _onGrounded += value; remove => _onGrounded -= value; }

        public event Action OnGainEnergy { add => _onGainEnergy += value; remove => _onGainEnergy -= value; }

        public event Action OnLoseEnergy { add => _onLoseEnergy += value; remove => _onLoseEnergy -= value; }

        /// <summary>
        /// implementation for INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler? Updated;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _status_system = gameObject.GetStatusSystem();
            _do_update = DoUpdate.GetInstance();
            _do_fixed_update = DoFixedUpdate.GetInstance();
            _acceleration = Acceleration.GetInstance(_forward_speed_limit, _run_speed_limit, _backward_speed_limit);
            _energy = Energy.GetInstance(_flight_power);
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            const float ACTION_POWER = 12.0f;

            /// <remarks>
            /// fRigidbody should be only used in FixedUpdate.
            /// </remarks>
            var rb = transform.GetComponent<Rigidbody>();

            // FIXME: to integrate with Energy function.
            this.FixedUpdateAsObservable().Subscribe(_ => {
                _acceleration.previousSpeed = _acceleration.currentSpeed;// hold previous speed.
                _acceleration.currentSpeed = rb.velocity.magnitude; // get speed.
            });

            // get vehicle speed for flight.
            Vector3 prev_position = transform.position;
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                airSpeed = ((transform.position - prev_position) / Time.deltaTime).magnitude * 3.6f; // m/s -> km/h
                verticalSpeed = ((transform.position.y - prev_position.y) / Time.deltaTime); // m/s
                prev_position = transform.position;
            });

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
                rb.AddFor​​ce(transform.forward * ACTION_POWER * 7.5f, ForceMode.Acceleration);
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
                rb.AddFor​​ce(transform.forward * ACTION_POWER * 7.5f, ForceMode.Acceleration);
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
                rb.AddFor​​ce(-transform.forward * ACTION_POWER * 7.5f, ForceMode.Acceleration);
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
                rb.useGravity = true;
                rb.AddRelativeFor​​ce(up * _jump_power * ACTION_POWER * 2, ForceMode.Acceleration);
                _do_fixed_update.CancelJump();
            });

            /// <summary>
            /// flight.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded).Subscribe(_ => {
                _energy.speed = airSpeed;
                _energy.altitude = transform.position.y - 0.5f; // 0.5 is half vehicle height.
                _do_fixed_update.ApplyFlight();
                _onFlight(); // call event handler.
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.flight).Subscribe(_ => {
                const float FALL_VALUE = 5.0f; // 5.0 -> -5m/s
                rb.useGravity = false;
                rb.velocity = transform.forward * _energy.GetCalculatePower();
                if (_use_lift_spoiler) {
                    var velocity = rb.velocity;
                    rb.velocity = new Vector3(velocity.x, velocity.y - FALL_VALUE, velocity.z);
                }
                _do_fixed_update.CancelFlight();
                _flight_stopwatch.Start();
            });

            /// <summary>
            /// gain energy.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _b_button.wasPressedThisFrame && !_do_update.grounded && _status_system.usePoint).Subscribe(_ => {
                _onGainEnergy();
                _energy.Gain();
            });

            /// <summary>
            /// lose energy.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _y_button.wasPressedThisFrame && !_do_update.grounded && _status_system.usePoint).Subscribe(_ => {
                _onLoseEnergy();
                _energy.Lose();
            });

            /// <summary>
            /// use or not use lift spoiler.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _a_button.wasPressedThisFrame && !_do_update.grounded).Subscribe(_ => {
                _use_lift_spoiler = !_use_lift_spoiler;
            });

            /// <summary>
            /// rotate(yaw).
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded).Subscribe(_ => {
                var axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                transform.Rotate(0, axis * (_rotational_speed * Time.deltaTime) * ACTION_POWER, 0);
            });

            /// <summary>
            /// pitch.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_up_button.isPressed || _down_button.isPressed)).Subscribe(_ => {
                var axis = _up_button.isPressed ? 1 : _down_button.isPressed ? -1 : 0;
                transform.Rotate(axis * (_pitch_speed * Time.deltaTime) * ACTION_POWER, 0, 0);
            });

            /// <summary>
            /// roll and yaw.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_button.isPressed || _right_button.isPressed)).Subscribe(_ => {
                var axis = _left_button.isPressed ? 1 : _right_button.isPressed ? -1 : 0;
                transform.Rotate(0, 0, axis * (_roll_speed * Time.deltaTime) * ACTION_POWER);
                axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                transform.Rotate(0, axis * (_roll_speed * Time.deltaTime) * ACTION_POWER, 0);
            });

            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_button.wasReleasedThisFrame || _right_button.wasReleasedThisFrame)).Subscribe(_ => {
            });

            /// <summary>
            /// stall.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && _energy.power < 1.0f && flightTime > 0.5f).Subscribe(_ => {
                Debug.Log($"stall");
                var ground_object = GameObject.Find("Ground");
                Quaternion ground_rotation = Quaternion.LookRotation(ground_object.transform.position);
                float speed = 2.5f;
                float step = speed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, ground_rotation, step);
                if (_energy.power < 10.0f || _energy.total < 10.0f || _energy.speed < 10.0f) {
                    _energy.Gain();
                }
            });

            /// <summary>
            /// freeze.
            /// </summary>
            this.OnCollisionStayAsObservable().Where(x => x.LikeBlock() && (_up_button.isPressed || _down_button.isPressed) && _acceleration.freeze).Subscribe(_ => {
                var reach = getReach();
                //Debug.Log("reach: " + Math.Round(transform.position.y, 2) % 1); // FIXME:
                if (_do_update.grounded && (reach < 0.5d || reach >= 0.99d)) {
                    moveLetfOrRight(getDirection(transform.forward));
                }
                else if (reach >= 0.5d && reach < 0.99d) {
                    rb.useGravity = false;
                    moveTop();
                }
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

                _use_lift_spoiler = false; // reset lift spoiler.
                _energy.hasLanded = true; // reset flight power.
                _flight_stopwatch.Reset(); // reset flight time.
                _onGrounded(); // call event handler.
            });

            /// <summary>
            /// when touching blocks.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(x => x.LikeBlock()).Subscribe(x => {
                if (!isHitSide(x.gameObject)) {
                    _do_update.grounded = true;
                    rb.useGravity = true;
                }
            });

            /// <summary>
            /// when leaving blocks.
            /// </summary>
            this.OnCollisionExitAsObservable().Where(x => x.LikeBlock()).Subscribe(x => {
                rb.useGravity = true;
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
            const float SPEED = 6.0f;
            transform.position = new(
                transform.position.x,
                transform.position.y + SPEED * Time.deltaTime,
                transform.position.z
            );
        }

        /// <summary>
        /// move aside when the vehicle hits a block.
        /// </summary>
        /// <param name="direction">the vehicle's direction is provided.</param>
        void moveLetfOrRight(Direction direction) {
            const float SPEED = 0.3f;
            Vector3 move_position = transform.position;
            // z-axis positive and negative.
            if (direction == Direction.PositiveZ || direction == Direction.NegativeZ) {
                if (transform.forward.x < 0f) {
                    move_position = new(
                        transform.position.x - SPEED * Time.deltaTime,
                        transform.position.y,
                        transform.position.z
                    );
                }
                else if (transform.forward.x >= 0f) {
                    move_position = new(
                        transform.position.x + SPEED * Time.deltaTime,
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
                        transform.position.z - SPEED * Time.deltaTime
                    );
                }
                else if (transform.forward.z >= 0f) {
                    move_position = new(
                        transform.position.x,
                        transform.position.y,
                        transform.position.z + SPEED * Time.deltaTime
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

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        #region DoUpdate

        /// <summary>
        /// class for the Update() method.
        /// </summary>
        class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _grounded;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool grounded {
                get => _grounded;
                set => _grounded = value;
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
                _grounded = false;
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

            bool _idol;
            bool _run;
            bool _walk;
            bool _jump;
            bool _backward;
            bool _flight;

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

            ///////////////////////////////////////////////////////////////////////////////////////////////
            // Constants

            const float TOTAL_POWAR_FACTOR_VALUE = 1.5f; // easy: 1.0f, normal: 1.5f , hard: 2.0f

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            float _flight_power;

            float _default_flight_power;

            float _calculated_power;

            float _altitude;

            Queue<float> _previous_altitudes = new();

            float _speed;

            float _total;

            float _threshold = 1f; // FIXME:

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float power {
                get {
                    return _calculated_power;
                }
            }

            public float altitude {
                set {
                    const int QUEUE_COUNT = 30;
                    if (_previous_altitudes.Count < QUEUE_COUNT) {
                        _previous_altitudes.Enqueue(_altitude);
                    }
                    else {
                        _previous_altitudes.Dequeue(); // keep the queue count.
                        _previous_altitudes.Enqueue(_altitude);
                    }
                    _altitude = value;
                }
            }

            /// <summary>
            /// speed in flighting.
            /// </summary>
            public float speed {
                get => _speed;
                set {
                    _speed = value;
                }
            }

            /// <summary>
            /// total energy.
            /// </summary>
            public float total {
                get => _altitude + _speed;
            }

            /// <summary>
            /// whether it has landed.
            /// </summary>
            public bool hasLanded {
                set {
                    if (value) {
                        _speed = 0;
                        _flight_power = _default_flight_power;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            //internal  Events [verb, verb phrase] 

            /// <summary>
            /// implementation for INotifyPropertyChanged
            /// </summary>
            internal event PropertyChangedEventHandler? Updated;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// hide the constructor.
            /// </summary>
            Energy(float flightPower) {
                _flight_power = flightPower;
                _default_flight_power = _flight_power;
            }

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static Energy GetInstance(float flightPower) {
                return new Energy(flightPower);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public float GetCalculatePower() {
                const float POWAR_FACTOR_VALUE = 4.0f;
                const float AUTO_FLARE_ALTITUDE = 8.0f;
                if (total > _threshold) {
                    if (_previous_altitudes.Peek() < _altitude) { // up
                        _flight_power -= 0.0009375f * POWAR_FACTOR_VALUE * TOTAL_POWAR_FACTOR_VALUE;
                    }
                    if (_previous_altitudes.Peek() > _altitude) { // down
                        _flight_power += 0.0009375f * POWAR_FACTOR_VALUE * TOTAL_POWAR_FACTOR_VALUE;
                    }
                }
                if (total <= _threshold && _altitude < AUTO_FLARE_ALTITUDE) {
                    _flight_power = _default_flight_power;
                }
                var result = _flight_power * 2.65f * TOTAL_POWAR_FACTOR_VALUE;
                _calculated_power = result < 0 ? 0 : result;
                return _calculated_power;
            }

            public void Gain() {
                _flight_power += 0.125f * TOTAL_POWAR_FACTOR_VALUE; ;
            }

            public void Lose() {
                _flight_power -= 0.125f * TOTAL_POWAR_FACTOR_VALUE; ;
            }
        }

        #endregion
    }
}
