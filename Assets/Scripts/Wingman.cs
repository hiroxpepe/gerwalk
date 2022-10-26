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

using static System.Math;
using UnityEngine;
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

using static Studio.MeowToon.Env;

namespace Studio.MeowToon {
    /// <summary>
    /// wingman class
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Wingman : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        GameObject _vehicle_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        Direction _vehicle_previous_direction;

        Quaternion _vehicle_rotation;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            /// <remarks>
            /// Rigidbody should be only used in FixedUpdate.
            /// </remarks>
            Rigidbody rb = transform.Get<Rigidbody>();

            // get vehicle.
            _vehicle_object = Find(name: VEHICLE_TYPE);
            Vehicle vehicle = _vehicle_object.Get<Vehicle>();

            /// <summary>
            /// vehicle updated.
            /// </summary>
            vehicle.Updated += (object sender, EvtArgs e) => {
                Vehicle vehicle = sender as Vehicle;
                if (vehicle is not null) {
                    if (e.Name.Equals(nameof(Vehicle.rotation))) { _vehicle_rotation = vehicle.rotation; return; }
                }
            };

            /// <summary>
            /// vehicle on flight.
            /// </summary>
            vehicle.OnFlight += () => { rb.useGravity = false; };

            /// <summary>
            /// vehicle on grounded.
            /// </summary>
            vehicle.OnGrounded += () => { rb.useGravity = true; };
        }

        // Start is called before the first frame update.
        void Start() {
            /// <remarks>
            /// Rigidbody should be only used in FixedUpdate.
            /// </remarks>
            Rigidbody rb = transform.Get<Rigidbody>();

            // FIXME: no use.
            _vehicle_previous_direction = getDirection(forward_vector: _vehicle_object.transform.forward);

            // Update is called once per frame.
            float move_time_count = 0f;
            this.UpdateAsObservable().Subscribe(onNext: _ => {
                Vector3 vehicle_position = _vehicle_object.transform.position;
                Direction vehicle_direction = getDirection(forward_vector: _vehicle_object.transform.forward);
                Vector3 move_position = getWingmanPosition(direction: vehicle_direction);
                if (vehicle_direction != _vehicle_previous_direction) {
                    _vehicle_previous_direction = vehicle_direction;
                    move_time_count = 0f; // reset time count.
                }
                move_time_count = moveWingmanPosition(move_position: move_position, time_count: move_time_count);
                transform.forward = _vehicle_object.transform.forward;
                transform.rotation = _vehicle_rotation;
            }).AddTo(this);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// get new position.
        /// </summary>
        Vector3 getWingmanPosition(Direction direction) {
            const float OFFSET_VALUE = 1.25f;
            Vector3 move_position = new(x: 0f, y: 0f, z: 0f);
            // z-axis positive.
            if (direction == Direction.PositiveZ ) {
                move_position = new(
                    x: _vehicle_object.transform.position.x + OFFSET_VALUE,
                    y: _vehicle_object.transform.position.y,
                    z: _vehicle_object.transform.position.z - OFFSET_VALUE
                );
            }
            // z-axis negative.
            if (direction == Direction.NegativeZ) {
                move_position = new(
                    x: _vehicle_object.transform.position.x - OFFSET_VALUE,
                    y: _vehicle_object.transform.position.y,
                    z: _vehicle_object.transform.position.z + OFFSET_VALUE
                );
            }
            // x-axis positive.
            if (direction == Direction.PositiveX) {
                move_position = new(
                    x: _vehicle_object.transform.position.x - OFFSET_VALUE,
                    y: _vehicle_object.transform.position.y,
                    z: _vehicle_object.transform.position.z - OFFSET_VALUE
                );
            }
            // x-axis negative.
            if (direction == Direction.NegativeX) {
                move_position = new(
                    x: _vehicle_object.transform.position.x + OFFSET_VALUE,
                    y: _vehicle_object.transform.position.y,
                    z: _vehicle_object.transform.position.z + OFFSET_VALUE
                );
            }
            // none.
            if (direction == Direction.None) {
                move_position = new(
                    x: _vehicle_object.transform.position.x + OFFSET_VALUE,
                    y: _vehicle_object.transform.position.y,
                    z: _vehicle_object.transform.position.z - OFFSET_VALUE
                );
            }
            return move_position;
        }

        /// <summary>
        /// move to new position.
        /// MEMO: time_count becomes over 1 but it works.
        /// </summary>
        float moveWingmanPosition(Vector3 move_position, float time_count) {
            time_count += Time.deltaTime;
            transform.position = Vector3.Slerp(a: transform.position, b: move_position, t: time_count);
            return time_count;
        }

        /// <summary>
        /// returns an enum of the vehicle's direction.
        /// </summary>
        Direction getDirection(Vector3 forward_vector) {
            float forward_x = (float) Round(a: forward_vector.x);
            float forward_y = (float) Round(a: forward_vector.y);
            float forward_z = (float) Round(a: forward_vector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // x-axis positive.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // x-axis negative.
            // determine the difference between the two axes.
            float absolute_x = Abs(value: forward_vector.x);
            float absolute_z = Abs(value: forward_vector.z);
            if (absolute_x > absolute_z) {
                if (forward_x == 1) { return Direction.PositiveX; } // x-axis positive.
                if (forward_x == -1) { return Direction.NegativeX; } // x-axis negative.
            }
            else if (absolute_x < absolute_z) {
                if (forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
                if (forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            }
            return Direction.None; // unknown.
        }
    }
}
