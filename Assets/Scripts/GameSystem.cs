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

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        const int FPS = 30; // 30fps
        const string MESSAGE_LEVEL_START = "Get items!";
        const string MESSAGE_LEVEL_CLEAR = "Level Clear!";
        const string MESSAGE_GAME_OVER = "Game Over!";
        const string MESSAGE_GAME_PAUSE = "Pause";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Text _message_text;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        bool _use_vibration = true;

        bool _is_pausing = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            Application.targetFrameRate = FPS;
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            // get scene name.
            var active_scene_name = SceneManager.GetActiveScene().name;

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
            /// pause the game execute or cancel.
            /// </summary>
            this.UpdateAsObservable().Where(_ => active_scene_name.Contains("Level") && _start_button.wasPressedThisFrame).Subscribe(_ => {
                if (_is_pausing) {
                    Time.timeScale = 1f; _message_text.text = "";
                } 
                else {
                    Time.timeScale = 0f; _message_text.text = MESSAGE_GAME_PAUSE; 
                }
                _is_pausing = !_is_pausing;
            });

            /// <summary>
            /// restart level.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _select_button.wasPressedThisFrame).Subscribe(_ => {
                SceneManager.LoadScene("Level1");
            });
        }
    }
}
