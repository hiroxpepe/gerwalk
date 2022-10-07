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
    /// lavel scene
    /// @author h.adachi
    /// </summary>
    public class Level : InputMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        bool _is_pausing = false;

        bool _is_home = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action? OnPauseOn;

        public event Action? OnPauseOff;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = gameObject.GetGameSystem();
            
            // get home.
            Home home = gameObject.GetHomeGameObject().GetHome();

            /// <summary>
            /// came back home.
            /// </summary>
            home.OnCameBack += () => {
                _is_home = true;
                Time.timeScale = 0f;
            };
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            /// <summary>
            /// pause the game execute or cancel.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _start_button.wasPressedThisFrame).Subscribe(_ => {
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

            // get targets count.
            _game_system.targetTotal = getTargetsCount();

            // check game status.
            this.UpdateAsObservable().Subscribe(_ => {
                checkGameStatus();
            });

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
                        SceneManager.LoadScene(Envelope.SCENE_ENDING);
                        break;
                }
            });

            /// <summary>
            /// restart game.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _select_button.wasPressedThisFrame).Subscribe(_ => {
                SceneManager.LoadScene(Envelope.SCENE_TITLE);
            });

            ///////////////////////////////////////////////////////////////////////////////////////////////
            // private Methods [verb]

            /// <summary>
            /// check game status
            /// </summary>
            void checkGameStatus() {
                _game_system.targetRemain = getTargetsCount();
            }

            /// <summary>
            /// get targets count.
            /// </summary>
            int getTargetsCount() {
                var targets = GameObject.Find(Envelope.TARGETS_OBJECT);
                Transform targets_transform = targets.GetComponentInChildren<Transform>();
                return targets_transform.childCount;
            }
        }
    }
}
