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
using static UnityEngine.GameObject;
using static Studio.MeowToon.Utils;

namespace Studio.MeowToon {
    /// <summary>
    /// game system
    /// </summary>
    /// <author>
    /// h.adachi (STUDIO MeowToon)
    /// </author>
    public class GameSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// game mode.
        /// </summary>
        public string mode { get => Status.mode; set => Status.mode = value; }

        /// <summary>
        /// point total.
        /// </summary>
        public int pointTotal { get => Status.pointTotal; set => Status.pointTotal = value; }

        /// <summary>
        /// can use points.
        /// </summary>
        public bool usePoint { get => Status.pointTotal > 0; }

        /// <summary>
        /// target total.
        /// </summary>
        public int targetTotal { get => Status.targetTotal; set => Status.targetTotal = value; }

        /// <summary>
        /// target remain.
        /// </summary>
        public int targetRemain { get => Status.targetRemain; set => Status.targetRemain = value; }

        /// <summary>
        /// beat the level.
        /// </summary>
        public bool beatLevel { get => Status.targetRemain == 0; }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action? OnPauseOn;

        public event Action? OnPauseOff;

        public event Action? OnIncrementPoints;

        public event Action? OnDecrementPoints;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        /// <summary>
        /// increment number of points.
        /// </summary>
        public void IncrementPoints() {
            const int POINT_VALUE = 5;
            pointTotal += POINT_VALUE;
            OnIncrementPoints?.Invoke();
        }

        /// <summary>
        /// decrement number of points.
        /// </summary>
        public void DecrementPoints() {
            pointTotal--;
            OnDecrementPoints?.Invoke();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            Application.targetFrameRate = Env.FPS;

            if (HasLevel()) {
                // get level.
                Level level = Find(name: Env.LEVEL_TYPE).Get<Level>();

                /// <summary>
                /// level pause on.
                /// </summary>
                level.OnPauseOn += () => { OnPauseOn?.Invoke(); };

                /// <summary>
                /// level pause off.
                /// </summary>
                level.OnPauseOff += () => { OnPauseOff?.Invoke(); };
            }

            if (HasVehicle()) {
                // get vehicle.
                Vehicle vehicle = Find(name: Env.VEHICLE_TYPE).Get<Vehicle>();

                /// <summary>
                /// vehicle on gain energy.
                /// spend points.
                /// </summary>
                vehicle.OnGainEnergy += () => { DecrementPoints(); };

                /// <summary>
                /// vehicle on lose energy.
                /// spend points.
                /// </summary>
                vehicle.OnLoseEnergy += () => { DecrementPoints(); };
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        #region Status

        static class Status {
#nullable enable

            ///////////////////////////////////////////////////////////////////////////////////////////
            // static Fields [nouns, noun phrases]

            static string _mode;

            static int _point_total, _target_total, _target_remain;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // static Constructor

            static Status() {
                _mode = Env.MODE_NORMAL;
                _point_total = 100;
                _target_total = _target_remain = 0;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public static Properties [noun, noun phrase, adjective]

            public static string mode {
                get => _mode; set => _mode = value;
            }

            public static int pointTotal {
                get => _point_total; set => _point_total = value;
            }

            public static int targetTotal {
                get => _target_total; set => _target_total = value;
            }

            public static int targetRemain {
                get => _target_remain; set => _target_remain = value;
            }
        }

        #endregion
    }
}
