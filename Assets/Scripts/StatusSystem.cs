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
    /// game system
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
        Text _speed_text;

        [SerializeField]
        Text _altitude_text;

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

        [SerializeField]
        GameObject _player_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        int _target_total = 0;

        int _target_remain = 0;

        int _point_total = 100;

        float _speed = 0f;

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

        // Start is called before the first frame update
        void Start() {
            // get targets count.
            _target_total = getTargetsCount();
            // update text ui.
            this.UpdateAsObservable().Subscribe(_ => {
                checkGameStatus();
                updateGameStatus();
                updatePlayerStatus();
            });
            // get player status.
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                var player = _player_object.gameObject.GetPlayer();
                _speed = player.flightSpeed;
                /// <remarks>
                /// for development.
                /// </remarks>
                _flight_time = player.flightTime;
                _energy = player.flightEnergy;
                _power = player.flightPower;
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
        /// update player status
        /// </summary>
        void updatePlayerStatus() {
            _speed_text.text = string.Format("TAS {0:000.0}km", Math.Round(_speed, 1, MidpointRounding.AwayFromZero));
            _altitude = _player_object.transform.position.y - 0.5f; // 0.5 is half player height.
            _altitude_text.text = string.Format("ALT {0:000.0}m", Math.Round(_altitude, 1, MidpointRounding.AwayFromZero));
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
