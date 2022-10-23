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

using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.GameObject;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using static Studio.MeowToon.Utils;

namespace Studio.MeowToon {
    /// <summary>
    /// select scene
    /// </summary>
    /// <author>
    /// h.adachi (STUDIO MeowToon)
    /// </author>
    public class Select : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const int SELECT_COUNT = 3;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Image _easy;

        [SerializeField]
        Image _normal;

        [SerializeField]
        Image _hard;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameSystem _game_system;

        Map<int, string> _focus = new();

        string _selected = Env.MODE_NORMAL;

        int _idx = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _game_system = Find(name: Env.GAME_SYSTEM).Get<GameSystem>();
            // set default focus.
            _focus.Add(0, Env.MODE_EASY);
            _focus.Add(1, Env.MODE_NORMAL);
            _focus.Add(2, Env.MODE_HARD);
            _idx = 1; // FIXME:
            _selected = _focus[_idx];
            changeSelectedColor();
        }

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            /// <summary>
            /// select up.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _up_button.wasPressedThisFrame).Subscribe(_ => {
                _idx--;
                if (_idx == -1) {
                    _idx = SELECT_COUNT - 1;
                }
                _selected = _focus[_idx];
                _game_system.mode = _selected;
                changeSelectedColor();
            });

            /// <summary>
            /// select down.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _down_button.wasPressedThisFrame || _select_button.wasPressedThisFrame).Subscribe(_ => {
                _idx++;
                if (_idx == SELECT_COUNT) {
                    _idx = 0;
                }
                _selected = _focus[_idx];
                _game_system.mode = _selected;
                changeSelectedColor();
            });

            /// <summary>
            /// return title.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _start_button.wasPressedThisFrame || _a_button.wasPressedThisFrame).Subscribe(_ => {
                SceneManager.LoadScene(Env.SCENE_TITLE);
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        void changeSelectedColor() {
            switch (_selected) {
                case Env.MODE_EASY:
                    _easy.color = yellow;
                    _normal.color = white;
                    _hard.color = white;
                    break;
                case Env.MODE_NORMAL:
                    _easy.color = white;
                    _normal.color = yellow;
                    _hard.color = white;
                    break;
                case Env.MODE_HARD:
                    _easy.color = white;
                    _normal.color = white;
                    _hard.color = yellow;
                    break;
            }
        }
    }
}
