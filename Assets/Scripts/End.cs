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

using UnityEngine.SceneManagement;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// end scene
    /// @author h.adachi
    /// </summary>
    public class End: GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = gameObject.GetGameSystem();
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            /// <summary>
            /// open select.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _select_button.wasPressedThisFrame).Subscribe(_ => {
            });

            /// <summary>
            /// start level.
            /// </summary>
            this.UpdateAsObservable().Where(_ => (_start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame)).Subscribe(_ => {
                SceneManager.LoadScene(Envelope.SCENE_TITLE);
            });
        }
    }
}
