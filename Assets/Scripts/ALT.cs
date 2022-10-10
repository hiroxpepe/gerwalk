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
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// ALT class
    /// @author h.adachi
    /// </summary>
    public class ALT : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        /// <summary>
        /// number to divide the circle.
        /// </summary>
        const int DIVIDE_CIRCLE_LONG = 100;
        const int DIVIDE_CIRCLE_SHORT = 1000;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        GameObject _long_needle_object;

        [SerializeField]
        GameObject _short_needle_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameObject _vehicle_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _vehicle_object = Find(Envelope.VEHICLE_TYPE);
        }

        // Start is called before the first frame update.
        void Start() {
            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
            });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(_ => {
                /// <summary>
                /// set altitude.
                /// </summary>
                const float VEHICLE_HEIGHT_OFFSET = 0.0f;
                float altitude = _vehicle_object.transform.position.y - VEHICLE_HEIGHT_OFFSET;
                _long_needle_object.transform.rotation = Quaternion.Euler(0f, 0f, -(360 / (DIVIDE_CIRCLE_LONG / altitude)));
                _short_needle_object.transform.rotation = Quaternion.Euler(0f, 0f, -(360 / (DIVIDE_CIRCLE_SHORT / altitude)));
            });
        }
    }
}
