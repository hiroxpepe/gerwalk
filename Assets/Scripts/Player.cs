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
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// player controller.
    /// @author h.adachi
    /// </summary>
    public class Player : GamepadMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////
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
        float _fly_power = 5.0f;

        [SerializeField]
        float _forward_speed_limit = 1.1f;

        [SerializeField]
        float _run_speed_limit = 3.25f;

        [SerializeField]
        float _backward_speed_limit = 0.75f;

        [SerializeField]
        SimpleAnimation _simple_anime;

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        DoUpdate _do_update;

        DoFixedUpdate _do_fixed_update;

        Acceleration _acceleration;

        Energy _energy;

        System.Diagnostics.Stopwatch _flying_stopwatch = new();

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// current speed of the player object to fly.
        /// </summary>
        public float flySpeed { get => _energy.speed; }

        /// <summary>
        /// current power of the player object to fly.
        /// </summary>
        public float flyPower { get => _energy.power; }

        /// <summary>
        /// elapsed time after takeoff.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float timeAfterTakeoff { get => (float) _flying_stopwatch.Elapsed.TotalSeconds; }

        /// <summary>
        /// total energy.
        /// </summary>
        /// <remarks>
        /// for development.
        /// </remarks>
        public float totalEnergy { get => _energy.total; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _do_update = DoUpdate.GetInstance();
            _do_fixed_update = DoFixedUpdate.GetInstance();
            _acceleration = Acceleration.GetInstance(_forward_speed_limit, _run_speed_limit, _backward_speed_limit);
            _energy = Energy.GetInstance(_fly_power);
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            const float POWER = 12.0f;

            var rb = transform.GetComponent<Rigidbody>(); // Rigidbody should be only used in FixedUpdate.

            this.FixedUpdateAsObservable().Subscribe(_ => {
                _acceleration.previousSpeed = _acceleration.currentSpeed;// hold previous speed.
                _acceleration.currentSpeed = rb.velocity.magnitude; // get speed.
            });

            // get player speed for fly.
            Vector3 prev_position = transform.position;
            float fly_speed = 0f;
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                fly_speed = ((transform.position - prev_position) / Time.deltaTime).magnitude * 3.6f;
                prev_position = transform.position;
            });

            /// <summary>
            /// idol.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && !_up_button.isPressed && !_down_button.isPressed)
                .Subscribe(_ => {
                    //_simpleAnime.Play("Default");
                    _do_fixed_update.ApplyIdol();
                });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.idol)
                .Subscribe(_ => {
                    rb.useGravity = true;
                });

            /// <summary>
            /// walk.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && _up_button.isPressed && !_y_button.isPressed).Subscribe(_ => {
                if (_do_update.grounded) { /*_simpleAnime.Play("Walk");*/ }
                _do_fixed_update.ApplyWalk();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.walk && _acceleration.walk).Subscribe(_ => {
                rb.AddFor​​ce(transform.forward * POWER * 7.5f, ForceMode.Acceleration);
                _do_fixed_update.CancelWalk();
            });

            /// <summary>
            /// run.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded && _up_button.isPressed && _y_button.isPressed).Subscribe(_ => {
                if (_do_update.grounded) { /*_simpleAnime.Play("Run");*/ }
                _do_fixed_update.ApplyRun();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.run && _acceleration.run).Subscribe(_ => {
                rb.AddFor​​ce(transform.forward * POWER * 7.5f, ForceMode.Acceleration);
                _do_fixed_update.CancelRun();
            });

            /// <summary>
            /// backward.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _down_button.isPressed).Subscribe(_ => {
                if (_do_update.grounded) { /*_simpleAnime.Play("Walk");*/ }
                _do_fixed_update.ApplyBackward();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.backward && _acceleration.backward).Subscribe(_ => {
                rb.AddFor​​ce(-transform.forward * POWER * 7.5f, ForceMode.Acceleration);
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
                rb.AddRelativeFor​​ce(up * _jump_power * POWER * 2, ForceMode.Acceleration);
                _do_fixed_update.CancelJump();
            });

            /// <summary>
            /// fly.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded).Subscribe(_ => {
                _energy.speed = fly_speed;
                _energy.altitude = transform.position.y - 0.5f; // 0.5 is half player height.
                _energy.timeAfterTakeoff = timeAfterTakeoff;
                _do_fixed_update.ApplyFly();
            });

            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.fly).Subscribe(_ => {
                rb.useGravity = false;
                rb.velocity = transform.forward * _energy.power;
                _do_fixed_update.CancelFly();
                _flying_stopwatch.Start();
            });

            /// <summary>
            /// gain energy.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _b_button.wasPressedThisFrame && !_do_update.grounded).Subscribe(_ => {
                _energy.Gain();
            });

            /// <summary>
            /// lose energy.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _y_button.wasPressedThisFrame && !_do_update.grounded).Subscribe(_ => {
                _energy.Lose();
            });

            /// <summary>
            /// rotate(yaw).
            /// </summary>
            this.UpdateAsObservable().Where(_ => _do_update.grounded).Subscribe(_ => {
                var axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                transform.Rotate(0, axis * (_rotational_speed * Time.deltaTime) * POWER, 0);
            });

            /// <summary>
            /// pitch.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_up_button.isPressed || _down_button.isPressed)).Subscribe(_ => {
                var axis = _up_button.isPressed ? 1 : _down_button.isPressed ? -1 : 0;
                transform.Rotate(axis * (_pitch_speed * Time.deltaTime) * POWER, 0, 0);
            });

            /// <summary>
            /// roll and yaw.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_button.isPressed || _right_button.isPressed)).Subscribe(_ => {
                var axis = _left_button.isPressed ? 1 : _right_button.isPressed ? -1 : 0;
                transform.Rotate(0, 0, axis * (_roll_speed * Time.deltaTime) * POWER);
                axis = _right_button.isPressed ? 1 : _left_button.isPressed ? -1 : 0;
                transform.Rotate(0, axis * (_roll_speed * Time.deltaTime) * POWER, 0);
            });

            this.UpdateAsObservable().Where(_ => !_do_update.grounded && (_left_button.wasReleasedThisFrame || _right_button.wasReleasedThisFrame)).Subscribe(_ => {
            });

            /// <summary>
            /// stall.
            /// </summary>
            this.UpdateAsObservable().Where(_ => !_do_update.grounded && _energy.total < 10.0f && timeAfterTakeoff > 3.0f).Subscribe(_ => {
                Debug.Log($"stall");
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

                // reset flying time.
                _flying_stopwatch.Reset();
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

            /// <summary>
            /// when touching balloons.
            /// </summary>
            //this.OnTriggerEnterAsObservable().Where(x => x.transform.name.Contains("Balloon")).Subscribe(x => {
            //    Destroy(x.gameObject);
            //});

            /// <summary>
            /// when touching balloons.
            /// </summary>
            this.OnTriggerEnterAsObservable().Where(x => x.transform.name.Contains("Balloon")).Subscribe(x => {
                x.gameObject.GetBalloon().DestroyWithItems(transform);
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
        /// move top when the player hits a block.
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
        /// move aside when the player hits a block.
        /// </summary>
        /// <param name="direction">the player's direction is provided.</param>
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
        /// returns an enum of the player's direction.
        /// </summary>
        Direction getDirection(Vector3 forwardVector) {
            var forward_x = (float) Math.Round(forwardVector.x);
            var forward_y = (float) Math.Round(forwardVector.y);
            var forward_z = (float) Math.Round(forwardVector.z);
            // z-axis positive.
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; }
            // z-axis negative.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; }
            // x-axis positive.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; }
            // x-axis negative.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; }
            // determine the difference between the two axes.
            float absolute_x = Math.Abs(forwardVector.x);
            float Absolute_z = Math.Abs(forwardVector.z);
            if (absolute_x > Absolute_z) {
                // x-axis positive.
                if (forward_x == 1) { return Direction.PositiveX; }
                // x-axis negative.
                if (forward_x == -1) { return Direction.NegativeX; }
            }
            else if (absolute_x < Absolute_z) {
                // z-axis positive.
                if (forward_z == 1) { return Direction.PositiveZ; }
                // z-axis negative.
                if (forward_z == -1) { return Direction.NegativeZ; }
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
            var y = transform.position.y;
            if (y < (target_top - ADJUST)) {
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
            bool _fly;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool idol { get => _idol; }
            public bool run { get => _run; }
            public bool walk { get => _walk; }
            public bool jump { get => _jump; }
            public bool backward { get => _backward; }
            public bool fly { get => _fly; }

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
                _run = _walk = _backward = _jump = _fly = false;
            }

            public void ApplyRun() {
                _idol = _walk = _backward = _fly = false;
                _run = true;
            }

            public void CancelRun() {
                _run = false;
            }

            public void ApplyWalk() {
                _idol = _run = _backward = _fly = false;
                _walk = true;
            }

            public void CancelWalk() {
                _walk = false;
            }

            public void ApplyBackward() {
                _idol = _run = _walk = _fly = false;
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

            public void ApplyFly() {
                _idol = _walk = _run = _backward = false;
                _fly = true;
            }

            public void CancelFly() {
                _fly = false;
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
            public bool walk { get => _current_speed < _forward_speed_limit; }
            public bool run { get => _current_speed < _run_speed_limit; }
            public bool backward { get => _current_speed < _backward_speed_limit; }
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

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            float _fly_power;

            float _default_fly_power;

            float _altitude;

            Queue<float> _previous_altitudes = new();

            float _speed;

            float _threshold = 750.0f;

            float _timeAfterTakeoff;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float power {
                get {
                    const float AUTO_FLARE_ALTITUDE = 8.0f;
                    if (total > _threshold /*|| timeAfterTakeoff > 3.0f*/) {
                        if (_previous_altitudes.Peek() < _altitude) { // up
                            _fly_power -= 0.025f;
                            //Debug.Log($"_flyPower : {_fly_power}");
                        }
                        if (_previous_altitudes.Peek() > _altitude) { // down
                            _fly_power += 0.010f;
                            //Debug.Log($"_flyPower : {_fly_power}");
                        }
                    }
                    if (total <= _threshold && _altitude < AUTO_FLARE_ALTITUDE) {
                        _fly_power = _default_fly_power;
                    }
                    return _fly_power * 2.65f;
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
            /// speed in flying.
            /// </summary>
            public float speed {
                get => _speed;
                set => _speed = value;
            }

            /// <summary>
            /// total energy.
            /// </summary>
            public float total {
                get => _altitude * _speed;
            }

            /// <summary>
            /// elapsed time after takeoff.
            /// </summary>
            public float timeAfterTakeoff {
                get => _timeAfterTakeoff;
                set => _timeAfterTakeoff = value; 
            }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// hide the constructor.
            /// </summary>
            Energy(float flyPower) {
                _fly_power = flyPower;
                _default_fly_power = _fly_power;
            }

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static Energy GetInstance(float flyPower) {
                return new Energy(flyPower);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public void Gain() {
                _fly_power += 0.375f;
            }

            public void Lose() {
                _fly_power -= 0.250f;
            }
        }

        #endregion
    }
}
