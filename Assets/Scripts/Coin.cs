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
using System.Linq;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// item coin class
    /// @author h.adachi
    /// </summary>
    public class Coin : Common {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Events [verb, verb phrase]

        public event Action? OnDestroy;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update.
        new void Start() {
            base.Start();

            /// <summary>
            /// wwhen being touched vehicle.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(x => x.LikeVehicle()).Subscribe(x => {
                OnDestroy?.Invoke();
                Destroy(gameObject);
            });
        }
    }
}
