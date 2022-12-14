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
using static UnityEngine.Quaternion;
using UniRx;
using UniRx.Triggers;

using static Studio.MeowToon.Env;

namespace Studio.MeowToon {
    /// <summary>
    /// TAS class
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class TAS : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        /// <summary>
        /// number to divide the circle.
        /// </summary>
        const int DIVIDE_CIRCLE = 200;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] GameObject _needle_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives]

        GameObject _vehicle_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _vehicle_object = Find(name: VEHICLE_TYPE);
        }

        // Start is called before the first frame update.
        void Start() {
            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(onNext: _ => {
                /// <summary>
                /// set speed.
                /// </summary>
                _needle_object.transform.rotation = Euler(x: 0f, y: 0f, z: -(360 / (DIVIDE_CIRCLE / _vehicle_object.Get<Vehicle>().airSpeed)));
            }).AddTo(this);
        }
    }
}
