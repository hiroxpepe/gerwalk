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

using System.Collections.Generic;
using static System.Math;
using UnityEngine;

using static Studio.MeowToon.Env;

namespace Studio.MeowToon {
    /// <summary>
    /// vehicle controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Vehicle : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region inner Classes

        class Energy {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            float _flight_power_base, _default_flight_power_base, _calculated_power, _altitude, _speed, _pitch;

            float _threshold = 1f; // FIXME:

            Queue<float> _previous_altitudes = new();

            bool _has_landed = false;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            /// <summary>
            /// altitude.
            /// </summary>
            public float altitude {
                set {
                    const int QUEUE_COUNT = FPS / 2; // 0.5 sec.
                    if (_previous_altitudes.Count < QUEUE_COUNT) {
                        _previous_altitudes.Enqueue(item: _altitude);
                    }
                    else {
                        _previous_altitudes.Dequeue(); // keep the queue count.
                        _previous_altitudes.Enqueue(item: _altitude);
                    }
                    _altitude = value; Updated?.Invoke(this, new(nameof(total)));
                }
            }

            /// <summary>
            /// speed in flighting.
            /// </summary>
            public float speed { get => _speed; set { _speed = value; Updated?.Invoke(this, new(nameof(total))); } }

            /// <summary>
            /// total energy.
            /// </summary>
            public float total { get => _altitude + _speed; }

            /// <summary>
            /// power for velocity.
            /// </summary>
            public float power { get => _calculated_power; }

            /// <summary>
            /// whether it has landed.
            /// </summary>
            public bool hasLanded {
                set {
                    _has_landed = value;
                    if (value) {
                        _speed = 0;
                        _flight_power_base = _default_flight_power_base;
                    }
                }
            }

            /// <summary>
            /// pitch
            /// </summary>
            public float pitch { set => _pitch = value; }

            /// <summary>
            /// ratio value to adjust pitch, roll, yaw speed.
            /// </summary>
            public float ratio {
                get {
                    const float REFERENCE_SPEED = 80f;
                    const float ADJUSTED_VALUE_1 = 0.01f;
                    const float ADJUSTED_VALUE_2 = 7.5f;
                    if (speed < 10.0f && !_has_landed) { return 1.0f; }
                    float ratio = 1.0f + ((speed - REFERENCE_SPEED) * ADJUSTED_VALUE_1 / ADJUSTED_VALUE_2);
                    Debug.Log($"speed: {speed} ratio: {ratio}");
                    return ratio;
                }
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
            Energy(float flight_power) {
                _flight_power_base = flight_power;
                _default_flight_power_base = _flight_power_base;
            }

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static Energy GetInstance(float flight_power) {
                return new Energy(flight_power);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            /// <summary>
            /// get the calculated power.
            /// </summary>
            public float GetCalculatedPower() {
                const float ADD_OR_SUBTRACT_VALUE = 0.0009375f;
                const float POWAR_FACTOR_VALUE = 5.0f;
                const float ADJUSTED_VALUE_1 = 25.0f;
                const float ADJUSTED_VALUE_2 = 2.65f;
                const float AUTO_FLARE_ALTITUDE = 8.0f;
                if (total > _threshold) {
                    float pitch_factor_value = 1.0f;
                    pitch_factor_value *= Abs(value: _pitch / ADJUSTED_VALUE_1);
                    float flight_value = ADD_OR_SUBTRACT_VALUE * POWAR_FACTOR_VALUE * _total_power_factor_value * pitch_factor_value;
                    if (_previous_altitudes.Peek() < _altitude) { // up
                        _flight_power_base -= flight_value;
                    }
                    if (_previous_altitudes.Peek() > _altitude) { // down
                        _flight_power_base += flight_value;
                    }
                }
                if (total <= _threshold && _altitude < AUTO_FLARE_ALTITUDE) {
                    _flight_power_base = _default_flight_power_base;
                }
                float power_value = _flight_power_base * ADJUSTED_VALUE_2 * _total_power_factor_value;
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
