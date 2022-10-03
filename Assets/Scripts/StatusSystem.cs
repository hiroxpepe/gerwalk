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
using System.ComponentModel;
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

        const string MESSAGE_LEVEL_START = "Get items!";
        const string MESSAGE_LEVEL_CLEAR = "Level Clear!";
        const string MESSAGE_GAME_OVER = "Game Over!";
        const string MESSAGE_GAME_PAUSE = "Pause";

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

        [SerializeField]
        Text _mode_text;

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

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        GameSystem _game_system;

        GameObject _vehicle_object;

        GameObject _home_object;

        float _air_speed = 0f;

        float _vertical_speed = 0f;

        float _altitude = 0f;

        bool _use_lift_spoiler = false;

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

        /// <remarks>
        /// color.
        /// </remarks>
        Color _red;
        Color _orange;
        Color _yellow;
        Color _lime;
        Color _green;
        Color _cyan;
        Color _azure;
        Color _blue;
        Color _purple;
        Color _magenta;
        Color _white;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// increment number of points.
        /// </summary>
        public void IncrementPoints() {
            const int POINT_VALUE = 5;
            _game_system.pointTotal += POINT_VALUE;
            updateGameStatus();
        }

        /// <summary>
        /// decrement number of points.
        /// </summary>
        public void DecrementPoints() {
            _game_system.pointTotal--;
            updateGameStatus();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = gameObject.GetGameSystem();
            _vehicle_object = gameObject.GetVehicleGameObject();
            Vehicle vehicle = _vehicle_object.GetVehicle();
            _home_object = gameObject.GetHomeGameObject();
            Home home = _home_object.GetHome();

            /// <summary>
            /// game system pause on.
            /// </summary>
            _game_system.OnPauseOn += () => {
                _message_text.text = MESSAGE_GAME_PAUSE;
            };

            /// <summary>
            /// game system pause off.
            /// </summary>
            _game_system.OnPauseOff += () => {
                _message_text.text = string.Empty;
            };

            /// <summary>
            /// vehicle updated.
            /// </summary>
            vehicle.Updated += (object sender, PropertyChangedEventArgs e) => {
                var vehicle = sender as Vehicle;
                if (vehicle is not null) {
                    if (e.PropertyName.Equals(nameof(Vehicle.airSpeed))) {
                        _air_speed = vehicle.airSpeed;
                    }
                    if (e.PropertyName.Equals(nameof(Vehicle.verticalSpeed))) {
                        _vertical_speed = vehicle.verticalSpeed;
                    }
                    if (e.PropertyName.Equals(nameof(Vehicle.flightTime))) {
                        _flight_time = vehicle.flightTime;
                    }
                    if (e.PropertyName.Equals(nameof(Vehicle.total))) {
                        _energy = vehicle.total;
                    }
                    if (e.PropertyName.Equals(nameof(Vehicle.power))) {
                        _power = vehicle.power;
                    }
                    if (e.PropertyName.Equals(nameof(Vehicle.useLiftSpoiler))) {
                        _use_lift_spoiler = vehicle.useLiftSpoiler;
                    }
                }
            };

            /// <summary>
            /// vehicle on gain energy.
            /// spend points.
            /// </summary>
            vehicle.OnGainEnergy += () => {
                DecrementPoints();
            };

            /// <summary>
            /// vehicle on lose energy.
            /// spend points.
            /// </summary>
            vehicle.OnLoseEnergy += () => {
                DecrementPoints();
            };

            /// <summary>
            /// came back home.
            /// </summary>
            home.OnCameBack += () => {
                _message_text.text = MESSAGE_LEVEL_CLEAR;
            };
        }

        // Start is called before the first frame update
        void Start() {
            // create color.
            createColor();

            // update text ui.
            this.UpdateAsObservable().Subscribe(_ => {
                updateGameStatus();
                updateVehicleStatus();
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// update game status
        /// </summary>
        void updateGameStatus() {
            _targets_text.text = string.Format("TGT {0}/{1}", _game_system.targetTotal - _game_system.targetRemain, _game_system.targetTotal);
            _points_text.text = string.Format("POINT {0}", _game_system.pointTotal);
            _mode_text.text = string.Format("Mode: {0}", _game_system.mode);
            switch (_game_system.mode) {
                case Envelope.MODE_EASY:
                    _mode_text.color = _yellow;
                    break;
                case Envelope.MODE_NORMAL:
                    _mode_text.color = _green;
                    break;
                case Envelope.MODE_HARD:
                    _mode_text.color = _purple;
                    break;
            }
        }

        /// <summary>
        /// update vehicle status
        /// </summary>
        void updateVehicleStatus() {
            _air_speed_text.text = string.Format("TAS {0:000.0}km/h", Math.Round(_air_speed, 1, MidpointRounding.AwayFromZero));
            _vertical_speed_text.text = string.Format("VSI {0:000.0}m/s", Math.Round(_vertical_speed, 1, MidpointRounding.AwayFromZero));
            _altitude = _vehicle_object.transform.position.y - 0.5f; // 0.5 is half vehicle height.
            _altitude_text.text = string.Format("ALT {0:000.0}m", Math.Round(_altitude, 1, MidpointRounding.AwayFromZero));
            var lift_spoiler_status = _use_lift_spoiler ? "ON" : "OFF";
            Color lift_spoiler_color = _use_lift_spoiler ? _red : _green;
            _lift_spoiler_text.text = $"Spoiler: {lift_spoiler_status}";
            _lift_spoiler_text.color = lift_spoiler_color;
            /// <remarks>
            /// for development.
            /// </remarks>
            setTotalEnergyColor(value: _energy);
            _energy_text.text = string.Format("ENG {0:0000.0}", Math.Round(_energy, 1, MidpointRounding.AwayFromZero));
            _power_text.text = string.Format("POW {0:0000.0}", Math.Round(_power, 1, MidpointRounding.AwayFromZero));
            _flight_text.text = string.Format("TIME {0:000.0}sec", Math.Round(_flight_time, 1, MidpointRounding.AwayFromZero));
        }

        /// <remarks>
        /// for development.
        /// </remarks>
        public void setTotalEnergyColor(float value) {
            const float START_VALUE = 20.0f;
            const float ADDED_VALUE = 20.0f;
            if (value is < START_VALUE) {
                _energy_text.color = _red;
            }
            else if (value is < START_VALUE + ADDED_VALUE and >= START_VALUE) {
                _energy_text.color = _orange;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 2 and >= START_VALUE + ADDED_VALUE) {
                _energy_text.color = _yellow;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 3 and >= START_VALUE + ADDED_VALUE * 2) {
                _energy_text.color = _lime;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 4 and >= START_VALUE + ADDED_VALUE * 3) {
                _energy_text.color = _green;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 5 and >= START_VALUE + ADDED_VALUE * 4) {
                _energy_text.color = _cyan;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 6 and >= START_VALUE + ADDED_VALUE * 5) {
                _energy_text.color = _azure;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 7 and >= START_VALUE + ADDED_VALUE * 6) {
                _energy_text.color = _blue;
            }
            else if (value is < START_VALUE + ADDED_VALUE * 8 and >= START_VALUE + ADDED_VALUE * 7) {
                _energy_text.color = _purple;
            }
            else {
                _energy_text.color = _magenta;
            }
        }

        /// <summary>
        /// create color.
        /// https://www.color-sample.com/colorschemes/rule/dominant/
        /// </summary>
        void createColor() {
            ColorUtility.TryParseHtmlString("#FF0000", out _red);
            ColorUtility.TryParseHtmlString("#FF7F00", out _orange);
            ColorUtility.TryParseHtmlString("#FFFF00", out _yellow);
            ColorUtility.TryParseHtmlString("#7FFF00", out _lime);
            ColorUtility.TryParseHtmlString("#00FF00", out _green);
            ColorUtility.TryParseHtmlString("#00FFFF", out _cyan);
            ColorUtility.TryParseHtmlString("#007FFF", out _azure);
            ColorUtility.TryParseHtmlString("#002AFF", out _blue);
            ColorUtility.TryParseHtmlString("#D400FF", out _purple);
            ColorUtility.TryParseHtmlString("#FF007F", out _magenta);
            ColorUtility.TryParseHtmlString("#FFFFFF", out _white);
        }
    }
}
