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
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;
using static Studio.MeowToon.Utils;

namespace Studio.MeowToon {
    /// <summary>
    /// status system
    /// @author h.adachi
    /// </summary>
    public class NoticeSystem : MonoBehaviour {
#nullable enable

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
        Text _heading_text;

        [SerializeField]
        Text _pitch_text;

        [SerializeField]
        Text _roll_text;

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

        float _air_speed, _vertical_speed, _altitude = 0f;

        bool _use_lift_spoiler = false;

        /// <summary>
        /// for development.
        /// </summary>
        float _energy, _power, _flight_time = 0f;

        float _heading, _pitch, _roll, _bank = 0f;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = gameObject.GetGameSystem();

            /// <summary>
            /// game system pause on.
            /// </summary>
            _game_system.OnPauseOn += () => {
                _message_text.text = Envelope.MESSAGE_GAME_PAUSE;
            };

            /// <summary>
            /// game system pause off.
            /// </summary>
            _game_system.OnPauseOff += () => {
                _message_text.text = string.Empty;
            };

            /// <summary>
            /// game system increment points.
            /// </summary>
            _game_system.OnIncrementPoints += () => {
                updateGameStatus();
            };

            /// <summary>
            /// game system decrement points.
            /// </summary>
            _game_system.OnDecrementPoints += () => {
                updateGameStatus();
            };

            // get vehicle.
            Vehicle vehicle = Find(Envelope.VEHICLE_TYPE).GetVehicle();

            /// <summary>
            /// vehicle updated.
            /// </summary>
            vehicle.Updated += (object sender, EvtArgs e) => {
                var vehicle = sender as Vehicle;
                if (vehicle is not null) {
                    if (e.Name.Equals(nameof(Vehicle.airSpeed))) { _air_speed = vehicle.airSpeed; return; }
                    if (e.Name.Equals(nameof(Vehicle.verticalSpeed))) { _vertical_speed = vehicle.verticalSpeed; return; }
                    if (e.Name.Equals(nameof(Vehicle.flightTime))) { _flight_time = vehicle.flightTime; return; }
                    if (e.Name.Equals(nameof(Vehicle.total))) { _energy = vehicle.total; return; }
                    if (e.Name.Equals(nameof(Vehicle.power))) { _power = vehicle.power; return; }
                    if (e.Name.Equals(nameof(Vehicle.useLiftSpoiler))) { _use_lift_spoiler = vehicle.useLiftSpoiler; return; }
                    if (e.Name.Equals(nameof(Vehicle.position))) { _altitude = vehicle.position.y - 0.5f; return; } // 0.5 is half vehicle height.
                    if (e.Name.Equals(nameof(Vehicle.rotation))) {
                        _heading = vehicle.heading; _pitch = vehicle.pitch; _roll = vehicle.roll; _bank = vehicle.bank; return;
                    }
                }
            };

            // get home.
            Home home = Find(Envelope.HOME_TYPE).GetHome();

            /// <summary>
            /// came back home.
            /// </summary>
            home.OnCameBack += () => {
                _message_text.text = Envelope.MESSAGE_LEVEL_CLEAR;
            };
        }

        // Start is called before the first frame update
        void Start() {
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
                case Envelope.MODE_EASY: _mode_text.color = yellow; break;
                case Envelope.MODE_NORMAL: _mode_text.color = green; break;
                case Envelope.MODE_HARD: _mode_text.color = purple; break;
            }
        }

        /// <summary>
        /// update vehicle status
        /// </summary>
        void updateVehicleStatus() {
            _air_speed_text.text = string.Format("TAS {0:000.0}km/h", Math.Round(_air_speed, 1, MidpointRounding.AwayFromZero));
            _vertical_speed_text.text = string.Format("VSI {0:000.0}m/s", Math.Round(_vertical_speed, 1, MidpointRounding.AwayFromZero));
            _altitude_text.text = string.Format("ALT {0:000.0}m", Math.Round(_altitude, 1, MidpointRounding.AwayFromZero));
            _heading_text.text = string.Format("HEADING {0:000.0}", Math.Round(_heading, 1, MidpointRounding.AwayFromZero));
            _pitch_text.text = string.Format("PITCH {0:000.0}", Math.Round(_pitch, 1, MidpointRounding.AwayFromZero));
            _roll_text.text = string.Format("BANK {0:000.0}", Math.Round(_bank, 1, MidpointRounding.AwayFromZero));
            _lift_spoiler_text.text = "Spoiler: " + (_use_lift_spoiler ? "ON" : "OFF");
            _lift_spoiler_text.color = _use_lift_spoiler ? red : green;
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
            if (value is < START_VALUE) { _energy_text.color = red; return; }
            if (value is < START_VALUE + ADDED_VALUE and >= START_VALUE) { _energy_text.color = orange; return; }
            if (value is < START_VALUE + ADDED_VALUE * 2 and >= START_VALUE + ADDED_VALUE) { _energy_text.color = yellow; return; }
            if (value is < START_VALUE + ADDED_VALUE * 3 and >= START_VALUE + ADDED_VALUE * 2) { _energy_text.color = lime; return; }
            if (value is < START_VALUE + ADDED_VALUE * 4 and >= START_VALUE + ADDED_VALUE * 3) { _energy_text.color = green; return; }
            if (value is < START_VALUE + ADDED_VALUE * 5 and >= START_VALUE + ADDED_VALUE * 4) { _energy_text.color = cyan; return; }
            if (value is < START_VALUE + ADDED_VALUE * 6 and >= START_VALUE + ADDED_VALUE * 5) { _energy_text.color = azure; return; }
            if (value is < START_VALUE + ADDED_VALUE * 7 and >= START_VALUE + ADDED_VALUE * 6) { _energy_text.color = blue; return; }
            if (value is < START_VALUE + ADDED_VALUE * 8 and >= START_VALUE + ADDED_VALUE * 7) { _energy_text.color = purple; return; }
             _energy_text.color = magenta; return;
        }
    }
}
