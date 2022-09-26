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

namespace Studio.MeowToon {
    /// <summary>
    /// Radar class.
    /// @author h.adachi
    /// </summary>
    public class Radar : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        GameObject player_object;

        [SerializeField]
        GameObject direction_object;

        ///////////////////////////////////////////////////////////////////////////
        // update Methods

        void LateUpdate() {
            Quaternion player_rotation = player_object.transform.rotation; // プレーヤーの y 軸を コンパスの z軸に設定する
            direction_object.transform.rotation = Quaternion.Euler(0f, 0f, -player_rotation.eulerAngles.y);
        }
    }
}
