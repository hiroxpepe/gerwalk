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

namespace Studio.MeowToon {
    /// <summary>
    /// vehicle controller
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public partial class Vehicle : InputMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        #region inner Classes

        /// <summary>
        /// class for the Update() method.
        /// </summary>
        class DoUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _grounded, _stall, _banking, _need_left_quick_roll, _need_right_quick_roll;

            ///////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool grounded { get => _grounded; set => _grounded = value; }

            public bool flighting { get => !_grounded; }

            public bool stalling { get => _stall; set => _stall = value; }

            public bool banking { get => _banking; set => _banking = value; }

            public bool needLeftQuickRoll { get => _need_left_quick_roll; set => _need_left_quick_roll = value; }

            public bool needRightQuickRoll { get => _need_right_quick_roll; set => _need_right_quick_roll = value; }

            ///////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static DoUpdate GetInstance() {
                DoUpdate instance = new();
                instance.ResetState();
                return instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            public void ResetState() {
                _grounded = _stall = _banking = _need_left_quick_roll = _need_right_quick_roll = false;
            }
        }

        #endregion
    }
}
