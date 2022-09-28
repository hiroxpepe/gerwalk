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

        const string MESSAGE_LEVEL_START = "Get items!";
        const string MESSAGE_LEVEL_CLEAR = "Level Clear!";
        const string MESSAGE_GAME_OVER = "Game Over!";
        const string MESSAGE_GAME_PAUSE = "Pause";

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Text message_text;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        bool is_pausing = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            // get scene name.
            var active_scene_name = SceneManager.GetActiveScene().name;

            /// <summary>
            /// pause the game execute or cancel.
            /// </summary>
            this.UpdateAsObservable().Where(_ => active_scene_name.Contains("Level") && _start_button.wasPressedThisFrame).Subscribe(_ => {
                if (is_pausing) {
                    Time.timeScale = 1f; message_text.text = "";
                } 
                else {
                    Time.timeScale = 0f; message_text.text = MESSAGE_GAME_PAUSE; 
                }
                is_pausing = !is_pausing;
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
