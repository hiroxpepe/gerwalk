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
using UnityEngine.SceneManagement;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// home class
    /// @author h.adachi
    /// </summary>
    public class Home : GamepadMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        bool _is_home = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action? OnCameBack;

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
            /// next level.
            /// </summary>
            this.UpdateAsObservable().Where(_ => (_a_button.wasPressedThisFrame) && _game_system.beatLevel && _is_home).Subscribe(_ => {
                switch (SceneManager.GetActiveScene().name) {
                    case Envelope.SCENE_LEVEL_1:
                        Time.timeScale = 1f;
                        SceneManager.LoadScene(Envelope.SCENE_LEVEL_2);
                        break;
                    case Envelope.SCENE_LEVEL_2:
                        Time.timeScale = 1f;
                        SceneManager.LoadScene(Envelope.SCENE_LEVEL_3);
                        break;
                    case Envelope.SCENE_LEVEL_3:
                        Time.timeScale = 1f;
                        SceneManager.LoadScene(Envelope.SCENE_End);
                        break;
                }
            });

            /// <summary>
            /// when being touched vehicle.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(x => x.LikeVehicle() && _game_system.beatLevel).Subscribe(x => {
                OnCameBack?.Invoke();
                _is_home = true;
                Time.timeScale = 0f;
            });
        }
    }
}
