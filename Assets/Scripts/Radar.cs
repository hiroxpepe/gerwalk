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
using UnityEngine.UI;
using static UnityEngine.GameObject;
using static UnityEngine.Quaternion;
using UniRx;
using UniRx.Triggers;

using static Studio.MeowToon.Env;

namespace Studio.MeowToon {
    /// <summary>
    /// radar class
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
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

        [SerializeField] GameObject _home_mark_object, _target_mark_object, _direction_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        GameObject _vehicle_object, _home_object, _targets_object;

        float _fast_cycle = 0.25f;

        float _slow_cycle = 1.0f;

        float _time;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _vehicle_object = Find(name: VEHICLE_TYPE);
            _home_object = Find(name: HOME_TYPE);
            _targets_object = Find(name: TARGETS_OBJECT);
        }

        // Start is called before the first frame update.
        void Start() {
            // hide default mark.
            _target_mark_object.Get<Image>().enabled = false;

            // get home and target positions.
            mapHomePositionsToRadar();
            mapTargetPositionsToRadar(create: true);

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(onNext: _ => {
                mapHomePositionsToRadar();
                mapTargetPositionsToRadar(create: false);
            });

            // LateUpdate is called after all Update functions have been called.
            this.LateUpdateAsObservable().Subscribe(onNext: _ => {
                /// <summary>
                /// set the vehicle's y-axis to the radar direction's z-axis.
                /// </summary>
                Quaternion vehicle_rotation = _vehicle_object.transform.rotation;
                _direction_object.transform.rotation = Euler(x: 0f, y: 0f, z: vehicle_rotation.eulerAngles.y);
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
            _home_mark_object.transform.localPosition = new Vector3(x: home_position.x * RANGE, y: home_position.z * RANGE, z: 0);
        }

        /// <summary>
        /// get target positions.
        /// </summary>
        void mapTargetPositionsToRadar(bool create = false) {

            // add the time.
            _time += Time.deltaTime;

            // get the value that repeats in cycle.
            float fast_repeat_value = Mathf.Repeat(t: _time, length: _fast_cycle);
            float slow_repeat_value = Mathf.Repeat(t: _time, length: _slow_cycle);

            // reset target mark.
            if (!create) {
                for (int reset_idx = 1; reset_idx < TARGETS_COUNT + 1; reset_idx++) {
                    GameObject target_mark = Find(name: $"RadarTarget(Clone)_{reset_idx}");
                    target_mark.Get<Image>().enabled = false;
                }
            }
            // set target mark.
            int idx = 1;
            foreach (Transform target_transform in _targets_object.transform) {
                Vector3 position = target_transform.position;
                GameObject target_mark = new();
                if (create) {
                    // create target mark.
                    target_mark = Instantiate(original: _target_mark_object);
                    target_mark.name += $"_{idx}";
                    target_mark.transform.SetParent(parent: _direction_object.transform, worldPositionStays: false);
                } else if (!create) {
                    // get target mark.
                    target_mark = Find(name: $"RadarTarget(Clone)_{idx}"); // FIXME:
                }
                // get target position from vehicle point of view.
                Vector3 target_position = target_transform.transform.position - _vehicle_object.transform.position;
                // map positions to radar.
                target_mark.transform.localPosition = new Vector3(x: target_position.x * RANGE, y: target_position.z * RANGE, z: 0);
                target_mark.Get<Image>().enabled = true;

                // higher altitude targets blink slowly, and lower altitude targets blink faster.
                Vector3 vehicle_position = _vehicle_object.transform.position;
                if (position.y < vehicle_position.y - ADJUSTED_VALUE) {
                    target_mark.Get<Image>().enabled = fast_repeat_value >= _fast_cycle * TO_MIDDLE_VALUE;
                } else if (position.y > vehicle_position.y + ADJUSTED_VALUE) {
                    target_mark.Get<Image>().enabled = slow_repeat_value >= _slow_cycle * TO_MIDDLE_VALUE;
                }
                idx++;
            }
        }
    }
}
