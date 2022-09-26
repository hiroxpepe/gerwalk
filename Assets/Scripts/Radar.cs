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
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// Radar class.
    /// @author h.adachi
    /// </summary>
    public class Radar : MonoBehaviour {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const int TARGETS_COUNT = 5;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        GameObject _player_object;

        [SerializeField]
        GameObject _direction_object;

        [SerializeField]
        GameObject _targets_object;

        [SerializeField]
        GameObject _target_mark_object;

        ///////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
        }

        // Start is called before the first frame update.
        void Start() {

            // hide default mark.
            _target_mark_object.GetComponent<Image>().enabled = false;

            // get target positions
            int idx = 1;
            foreach (Transform target_transform in _targets_object.transform) {
                var position = target_transform.position;
                Debug.Log($"position.x: {position.x} position.y: {position.y} position.z: {position.z}");
                var target_mark = Instantiate(_target_mark_object);
                target_mark.name += $"_{idx}";
                target_mark.transform.SetParent(transform, false);
                target_mark.transform.localPosition = new Vector3(0, 0, 0);
                target_mark.GetComponent<Image>().enabled = true;
                idx++;
            }

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
            });
        }

        void LateUpdate() {
            Quaternion player_rotation = _player_object.transform.rotation; // set the player's y-axis to the radar direction's z-axis
            _direction_object.transform.rotation = Quaternion.Euler(0f, 0f, -player_rotation.eulerAngles.y);
        }
    }
}
