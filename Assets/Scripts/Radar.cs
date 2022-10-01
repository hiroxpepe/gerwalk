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

using static UnityEngine.GameObject;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// radar class
    /// @author h.adachi
    /// </summary>
    public class Radar : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const float RANGE = 2.0f;

        const int TARGETS_COUNT = 5;

        const float TO_MIDDLE_VALUE = 0.5f;

        const int ADJUSTED_VALUE = 10;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        GameObject _home_object;

        [SerializeField]
        GameObject _home_mark_object;

        [SerializeField]
        GameObject _targets_object;

        [SerializeField]
        GameObject _target_mark_object;

        [SerializeField]
        GameObject _direction_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        GameObject _vehicle_object;

        float _fast_cycle = 0.25f;

        float _slow_cycle = 1.0f;

        float _time;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _vehicle_object = gameObject.GetVehicleGameObject();
        }

        // Start is called before the first frame update.
        void Start() {
            // hide default mark.
            _target_mark_object.GetImage().enabled = false;

            // get home and target positions.
            mapHomePositionsToRadar();
            mapTargetPositionsToRadar(create: true);

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
                mapHomePositionsToRadar();
                mapTargetPositionsToRadar(create: false);
            });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(_ => {
                /// <summary>
                /// set the vehicle's y-axis to the radar direction's z-axis.
                /// </summary>
                Quaternion vehicle_rotation = _vehicle_object.transform.rotation;
                _direction_object.transform.rotation = Quaternion.Euler(0f, 0f, vehicle_rotation.eulerAngles.y);
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// get home position.
        /// </summary>
        void mapHomePositionsToRadar() {
            // get home position from vehicle point of view.
            Vector3 home_position = _home_object.transform.position - _vehicle_object.transform.position;
            // map position to radar.
            _home_mark_object.transform.localPosition = new Vector3(home_position.x * RANGE, home_position.z * RANGE, 0);
        }

        /// <summary>
        /// get target positions.
        /// </summary>
        void mapTargetPositionsToRadar(bool create = false) {

            // add the time.
            _time += Time.deltaTime;

            // get the value that repeats in cycle.
            var fast_repeat_value = Mathf.Repeat(_time, _fast_cycle);
            var slow_repeat_value = Mathf.Repeat(_time, _slow_cycle);

            // reset target mark.
            if (!create) {
                for (int reset_idx = 1; reset_idx < TARGETS_COUNT + 1; reset_idx++) {
                    var target_mark = Find($"RadarTarget(Clone)_{reset_idx}");
                    target_mark.GetImage().enabled = false;
                }
            }
            // set target mark.
            int idx = 1;
            foreach (Transform target_transform in _targets_object.transform) {
                Vector3 position = target_transform.position;
                GameObject target_mark = new();
                if (create) {
                    // create target mark.
                    target_mark = Instantiate(_target_mark_object);
                    target_mark.name += $"_{idx}";
                    target_mark.transform.SetParent(_direction_object.transform, false);
                } else if (!create) {
                    // get target mark.
                    target_mark = Find($"RadarTarget(Clone)_{idx}"); // FIXME:
                }
                // get target position from vehicle point of view.
                Vector3 target_position = target_transform.transform.position - _vehicle_object.transform.position;
                // map positions to radar.
                target_mark.transform.localPosition = new Vector3(target_position.x * RANGE, target_position.z * RANGE, 0);
                target_mark.GetImage().enabled = true;

                // higher altitude targets blink slowly, and lower altitude targets blink faster.
                Vector3 vehicle_position = _vehicle_object.transform.position;
                if (position.y < vehicle_position.y - ADJUSTED_VALUE) {
                    target_mark.GetImage().enabled = fast_repeat_value >= _fast_cycle * TO_MIDDLE_VALUE;
                } else if (position.y > vehicle_position.y + ADJUSTED_VALUE) {
                    target_mark.GetImage().enabled = slow_repeat_value >= _slow_cycle * TO_MIDDLE_VALUE;
                }
                idx++;
            }
        }
    }
}
