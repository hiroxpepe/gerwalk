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
using UnityEngine.SceneManagement;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// game system
    /// @author h.adachi
    /// </summary>
    public class GameSystem : GamepadMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string TARGETS_OBJECT = "Balloons"; // name of target objects holder.

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // static Fields [noun, adjectives] 

        static string _mode = Envelope.MODE_NORMAL;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        bool _is_pausing = false;

        string _active_scene_name = string.Empty;

        int _point_total = 100;

        int _target_total = 0;

        int _target_remain = 0;

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
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            // get scene name.
            _active_scene_name = SceneManager.GetActiveScene().name;

            // get targets count.
            _target_total = getTargetsCount();

            // check game status.
            this.UpdateAsObservable().Subscribe(_ => {
                checkGameStatus();
            });

            /// <summary>
            /// pause the game execute or cancel.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _start_button.wasPressedThisFrame && isPlayLevel()).Subscribe(_ => {
                if (_is_pausing) {
                    Time.timeScale = 1f;
                    OnPauseOff?.Invoke();
                } 
                else {
                    Time.timeScale = 0f;
                    OnPauseOn?.Invoke();
                }
                _is_pausing = !_is_pausing;
            });

            /// <summary>
            /// restart game.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _select_button.wasPressedThisFrame && isPlayLevel()).Subscribe(_ => {
                SceneManager.LoadScene(Envelope.SCENE_TITLE);
            });

            ///////////////////////////////////////////////////////////////////////////////////////////////
            // private Methods [verb]

            bool isPlayLevel() {
                return _active_scene_name.Contains("Level");
            }

            /// <summary>
            /// check game status
            /// </summary>
            void checkGameStatus() {
                _target_remain = getTargetsCount();
            }

            /// <summary>
            /// get targets count.
            /// </summary>
            int getTargetsCount() {
                var targets = GameObject.Find(TARGETS_OBJECT);
                Transform targets_transform = targets.GetComponentInChildren<Transform>();
                return targets_transform.childCount;
            }
        }
    }
}
