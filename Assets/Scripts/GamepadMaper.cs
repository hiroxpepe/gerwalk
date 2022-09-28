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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// to map physical Gamepad.
    /// @author h.adachi
    /// </summary>
    public class GamepadMaper : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        protected GameObject _v_controller_object;

        protected ButtonControl _a_button;

        protected ButtonControl _b_button;

        protected ButtonControl _x_button;

        protected ButtonControl _y_button;

        protected ButtonControl _up_button;

        protected ButtonControl _down_button;

        protected ButtonControl _left_button;

        protected ButtonControl _right_button;

        protected ButtonControl _right_stick_up_button;

        protected ButtonControl _right_stick_down_button;

        protected ButtonControl _right_stick_left_button;

        protected ButtonControl _right_stick_right_button;

        protected ButtonControl _right_stick_button;

        bool _use_v_controller;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        /// <summary>
        /// whether to use virtual controllers.
        /// </summary>
        public bool useVirtualController { get => _use_v_controller; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        protected void Start() {
            // get virtual controller object.
            _v_controller_object = GameObject.Find("VController");

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
                mapGamepad();
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        void mapGamepad() {

            // check a physical gamepad connected.
            var controller_names = Input.GetJoystickNames();
            if (controller_names.Length == 0 || controller_names[0] == "") {
                _v_controller_object.SetActive(true);
                _use_v_controller = true;
            }
            else {
                _v_controller_object.SetActive(false);
                _use_v_controller = false;
            }

            // identifies the OS.
            _up_button = Gamepad.current.dpad.up;
            _down_button = Gamepad.current.dpad.down;
            _left_button = Gamepad.current.dpad.left;
            _right_button = Gamepad.current.dpad.right;
            if (Application.platform == RuntimePlatform.Android) {
                // Android OS
                _a_button = Gamepad.current.aButton;
                _b_button = Gamepad.current.bButton;
                _x_button = Gamepad.current.xButton;
                _y_button = Gamepad.current.yButton;
            }
            else if (Application.platform == RuntimePlatform.WindowsPlayer) {
                // Windows OS
                _a_button = Gamepad.current.bButton;
                _b_button = Gamepad.current.aButton;
                _x_button = Gamepad.current.yButton;
                _y_button = Gamepad.current.xButton;
            }
            else {
                // FIXME: can't get it during development with Unity?
                _a_button = Gamepad.current.bButton;
                _b_button = Gamepad.current.aButton;
                _x_button = Gamepad.current.yButton;
                _y_button = Gamepad.current.xButton;
            }
            _right_stick_up_button = Gamepad.current.rightStick.up;
            _right_stick_down_button = Gamepad.current.rightStick.down;
            _right_stick_left_button = Gamepad.current.rightStick.left;
            _right_stick_right_button = Gamepad.current.rightStick.right;
            _right_stick_button = Gamepad.current.rightStickButton;
        }
    }
}
