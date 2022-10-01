/*
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// status system
    /// @author h.adachi
    /// </summary>
    public class StatusSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string TARGETS_OBJECT = "Balloons"; // name of target objects holder.

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Text _message_text;

        [SerializeField]
        Text _targets_text;

        [SerializeField]
        Text _points_text;

        [SerializeField]
        Text _air_speed_text;

        [SerializeField]
        Text _vertical_speed_text;

        [SerializeField]
        Text _altitude_text;

        [SerializeField]
        Text _lift_spoiler_text;

        /// <remarks>
        /// for development.
        /// </remarks>
        [SerializeField]
        Text _energy_text;

        /// <remarks>
        /// for development.
        /// </remarks>
        [SerializeField]
        Text _power_text;

        /// <remarks>
        /// for development.
        /// </remarks>
        [SerializeField]
        Text _flight_text;

        /// <remarks>
        /// for development.
        /// </remarks>
        [SerializeField]
        Text _debug_text;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        GameObject _vehicle_object;

        int _target_total = 0;

        int _target_remain = 0;

        int _point_total = 100;

        float _air_speed = 0f;

        float _vertical_speed = 0f;

        float _altitude = 0f;

        /// <remarks>
        /// for development.
        /// </remarks>
        float _energy = 0f;

        /// <remarks>
        /// for development.
        /// </remarks>
        float _power = 0;

        /// <remarks>
        /// for development.
        /// </remarks>
        float _flight_time = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        public bool usePoint {
            get {
                return _point_total > 0 ? true : false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// increment number of points.
        /// </summary>
        public void IncrementPoints() {
            const int POINT_VALUE = 5;
            _point_total += POINT_VALUE;
            updateGameStatus();
        }

        /// <summary>
        /// decrement number of points.
        /// </summary>
        public void DecrementPoints() {
            _point_total--;
            updateGameStatus();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _vehicle_object = gameObject.GetVehicleGameObject();
            var vehicle = _vehicle_object.GetVehicle();

            /// <summary>
            /// spend points.
            /// </summary>
            vehicle.OnGainEnergy += () => {
                DecrementPoints();
            };

            /// <summary>
            /// spend points.
            /// </summary>
            vehicle.OnLoseEnergy += () => {
                DecrementPoints();
            };
        }

        // Start is called before the first frame update
        void Start() {
            // get targets count.
            _target_total = getTargetsCount();

            // update text ui.
            this.UpdateAsObservable().Subscribe(_ => {
                checkGameStatus();
                updateGameStatus();
                updateVehicleStatus();
            });

            // get vehicle status.
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                var vehicle = _vehicle_object.gameObject.GetVehicle();
                _air_speed = vehicle.flightSpeed;
                _vertical_speed = vehicle.flightVerticalSpeed;
                /// <remarks>
                /// for development.
                /// </remarks>
                _flight_time = vehicle.flightTime;
                _energy = vehicle.flightEnergy;
                _power = vehicle.flightPower;
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// check game status
        /// </summary>
        void checkGameStatus() {
            _target_remain = getTargetsCount();
        }

        /// <summary>
        /// update game status
        /// </summary>
        void updateGameStatus() {
            _targets_text.text = string.Format("TGT {0}/{1}", _target_total - _target_remain, _target_total);
            _points_text.text = string.Format("POINT {0}", _point_total);
        }

        /// <summary>
        /// update vehicle status
        /// </summary>
        void updateVehicleStatus() {
            _air_speed_text.text = string.Format("TAS {0:000.0}km/h", Math.Round(_air_speed, 1, MidpointRounding.AwayFromZero));
            _vertical_speed_text.text = string.Format("VSI {0:000.0}m/s", Math.Round(_vertical_speed, 1, MidpointRounding.AwayFromZero));
            _altitude = _vehicle_object.transform.position.y - 0.5f; // 0.5 is half vehicle height.
            _altitude_text.text = string.Format("ALT {0:000.0}m", Math.Round(_altitude, 1, MidpointRounding.AwayFromZero));
            var vehicle = _vehicle_object.gameObject.GetVehicle();
            var lift_spoiler_status = vehicle.useLiftSpoiler ? "ON" : "OFF";
            var lift_spoiler_color_code = vehicle.useLiftSpoiler ? "#FF0000" : "#00FF00";
            Color lift_spoiler_color;
            ColorUtility.TryParseHtmlString(lift_spoiler_color_code, out lift_spoiler_color);
            _lift_spoiler_text.text = $"Spoiler: {lift_spoiler_status}";
            _lift_spoiler_text.color = lift_spoiler_color;
            /// <remarks>
            /// for development.
            /// </remarks>
            setTotalEnergyColor(_energy);
            _energy_text.text = string.Format("ENG {0:0000.0}", Math.Round(_energy, 1, MidpointRounding.AwayFromZero));
            _power_text.text = string.Format("POW {0:0000.0}", Math.Round(_power, 1, MidpointRounding.AwayFromZero));
            _flight_text.text = string.Format("TIME {0:000.0}sec", Math.Round(_flight_time, 1, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// get targets count.
        /// </summary>
        int getTargetsCount() {
            var targets = GameObject.Find(TARGETS_OBJECT);
            Transform targets_transform = targets.GetComponentInChildren<Transform>();
            return targets_transform.childCount;
        }

        /// <remarks>
        /// for development.
        /// </remarks>
        public void setTotalEnergyColor(float value) {
            // https://www.color-sample.com/colorschemes/rule/dominant/
            const float START_VALUE = 20.0f;
            const float ADDED_VALUE = 20.0f;
            Color color;
            if (value is < START_VALUE) {
                ColorUtility.TryParseHtmlString("#FF0000", out color); // red
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE and >= START_VALUE) {
                ColorUtility.TryParseHtmlString("#FF7F00", out color); // orange
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 2 and >= START_VALUE + ADDED_VALUE) {
                ColorUtility.TryParseHtmlString("#FFFF00", out color); // yellow
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 3 and >= START_VALUE + ADDED_VALUE * 2) {
                ColorUtility.TryParseHtmlString("#7FFF00", out color); // lime
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 4 and >= START_VALUE + ADDED_VALUE * 3) {
                ColorUtility.TryParseHtmlString("#00FF00", out color); // green
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 5 and >= START_VALUE + ADDED_VALUE * 4) {
                ColorUtility.TryParseHtmlString("#00FFFF", out color); // cyan
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 6 and >= START_VALUE + ADDED_VALUE * 5) {
                ColorUtility.TryParseHtmlString("#007FFF", out color); // azure
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 7 and >= START_VALUE + ADDED_VALUE * 6) {
                ColorUtility.TryParseHtmlString("#002AFF", out color); // blue
                _energy_text.color = color;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 8 and >= START_VALUE + ADDED_VALUE * 7) {
                ColorUtility.TryParseHtmlString("#D400FF", out color); // purple
                _energy_text.color = color;
            }
            else {
                ColorUtility.TryParseHtmlString("#FF007F", out color); // magenta
                _energy_text.color = color;
            }
        }

        /// <summary>
        /// debug trace
        /// </summary>
        public void trace(string value) {
            _debug_text.text = value;
        }
    }
}
