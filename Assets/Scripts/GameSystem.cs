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
        // static Fields [noun, adjectives] 

        static string _mode = Envelope.MODE_NORMAL;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        bool _use_vibration = true;

        bool _is_pausing = false;

        string _active_scene_name = string.Empty;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// game mode.
        /// </summary>
        public string mode { get => _mode; set => _mode = value; }

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

            #region mobile phone vibration.

            // vibrate the smartphone when the button is pressed.
            this.UpdateAsObservable().Where(_ => _v_controller_object && _use_vibration &&
                (_a_button.wasPressedThisFrame || _b_button.wasPressedThisFrame || _x_button.wasPressedThisFrame || _y_button.wasPressedThisFrame ||
                _up_button.wasPressedThisFrame || _down_button.wasPressedThisFrame || _left_button.wasPressedThisFrame || _right_button.wasPressedThisFrame ||
                _left_1_button.wasPressedThisFrame || _right_1_button.wasPressedThisFrame || _select_button.wasPressedThisFrame || _start_button.wasPressedThisFrame)).Subscribe(_ => {
                AndroidVibrator.Vibrate(50L);
            });

            // no vibration of the smartphone by pressing the start and X buttons at the same time.
            this.UpdateAsObservable().Where(_ => (_x_button.isPressed && _start_button.wasPressedThisFrame) || (_x_button.wasPressedThisFrame && _start_button.isPressed)).Subscribe(_ => {
                _use_vibration = !_use_vibration;
            });

            #endregion

            /// <summary>
            /// open select.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _select_button.wasPressedThisFrame && !isPlayLevel()).Subscribe(_ => {
                SceneManager.LoadScene(Envelope.SCENE_SELECT);
            });

            /// <summary>
            /// start level.
            /// </summary>
            this.UpdateAsObservable().Where(_ => (_start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame) && !isPlayLevel()).Subscribe(_ => {
                SceneManager.LoadScene(Envelope.SCENE_LEVEL_1);
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
        }
    }
}
