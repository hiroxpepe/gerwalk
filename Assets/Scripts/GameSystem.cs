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

namespace Studio.MeowToon {
    /// <summary>
    /// game system
    /// @author h.adachi
    /// </summary>
    public class GameSystem : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // static Fields [noun, adjectives] 

        static string _mode = Envelope.MODE_NORMAL;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        int _point_total = 100;

        int _target_total = 0;

        int _target_remain = 0;

        GameObject _level_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// game mode.
        /// </summary>
        public string mode { get => _mode; set => _mode = value; }

        /// <summary>
        /// point total.
        /// </summary>
        public int pointTotal { get => _point_total; set => _point_total = value; }

        /// <summary>
        /// can use points.
        /// </summary>
        public bool usePoint {
            get {
                return _point_total > 0;
            }
        }

        /// <summary>
        /// target total.
        /// </summary>
        public int targetTotal { get => _target_total; set => _target_total = value; }

        /// <summary>
        /// target remain.
        /// </summary>
        public int targetRemain { get => _target_remain; set => _target_remain = value; }

        /// <summary>
        /// beat the level.
        /// </summary>
        public bool beatLevel {
            get {
                return _target_remain == 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action? OnPauseOn;

        public event Action? OnPauseOff;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            Application.targetFrameRate = Envelope.FPS;

            // level
            if (hasLevel()) {
                _level_object = gameObject.GetLevelGameObject();
                Level level = _level_object.GetLevel();

                /// <summary>
                /// level pause on.
                /// </summary>
                level.OnPauseOn += () => {
                    OnPauseOn?.Invoke();
                };

                /// <summary>
                /// level pause off.
                /// </summary>
                level.OnPauseOff += () => {
                    OnPauseOff?.Invoke();
                };
            }
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// has level.
        /// </summary>
        bool hasLevel() {
            GameObject level_object = GameObject.Find("Level");
            if (level_object is not null) {
                return true;
            }
            return false;
        }
    }
}
