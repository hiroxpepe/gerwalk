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

using System;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// Wingman controller.
    /// @author h.adachi
    /// </summary>
    public class Wingman : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        GameObject _vehicle_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        Direction _vehicle_previous_direction;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _vehicle_object = gameObject.GetVehicleGameObject();
            var vehicle = _vehicle_object.GetVehicle();

            /// <remarks>
            /// fRigidbody should be only used in FixedUpdate.
            /// </remarks>
            var rb = transform.GetComponent<Rigidbody>();

            /// <summary>
            /// vehicle on flight.
            /// </summary>
            vehicle.OnFlight += () => {
                rb.useGravity = false;
            };

            /// <summary>
            /// vehicle on grounded.
            /// </summary>
            vehicle.OnGrounded += () => {
                rb.useGravity = true;
            };
        }

        // Start is called before the first frame update.
        void Start() {

            /// <remarks>
            /// fRigidbody should be only used in FixedUpdate.
            /// </remarks>
            var rb = transform.GetComponent<Rigidbody>();

            // FIXME: no use.
            _vehicle_previous_direction = getDirection(_vehicle_object.transform.forward);

            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
                var vehicle_position = _vehicle_object.transform.position;
                Direction vehicle_direction = getDirection(_vehicle_object.transform.forward);
                Vector3 move_position = getWingmanPosition(vehicle_direction);
                if (vehicle_direction != _vehicle_previous_direction) {
                    //Debug.Log($"Changed! vehicle_direction: {vehicle_direction}");
                    _vehicle_previous_direction = vehicle_direction;
                }
                moveWingmanPosition(move_position);
                transform.forward = _vehicle_object.transform.forward;
            });

            this.FixedUpdateAsObservable().Subscribe(_ => {
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// get new position.
        /// </summary>
        Vector3 getWingmanPosition(Direction direction) {
            const float SHIFT = 2.5f;
            Vector3 move_position = new(0f, 0f, 0f);
            // z-axis positive.
            if (direction == Direction.PositiveZ ) {
                move_position = new(
                    _vehicle_object.transform.position.x + SHIFT,
                    _vehicle_object.transform.position.y,
                    _vehicle_object.transform.position.z - SHIFT
                );
            }
            // z-axis negative.
            if (direction == Direction.NegativeZ) {
                move_position = new(
                    _vehicle_object.transform.position.x - SHIFT,
                    _vehicle_object.transform.position.y,
                    _vehicle_object.transform.position.z + SHIFT
                );
            }
            // x-axis positive.
            if (direction == Direction.PositiveX) {
                move_position = new(
                    _vehicle_object.transform.position.x - SHIFT,
                    _vehicle_object.transform.position.y,
                    _vehicle_object.transform.position.z - SHIFT
                );
            }
            // x-axis negative.
            if (direction == Direction.NegativeX) {
                move_position = new(
                    _vehicle_object.transform.position.x + SHIFT,
                    _vehicle_object.transform.position.y,
                    _vehicle_object.transform.position.z + SHIFT
                );
            }
            // none.
            if (direction == Direction.None) {
                move_position = new(
                    _vehicle_object.transform.position.x + SHIFT,
                    _vehicle_object.transform.position.y,
                    _vehicle_object.transform.position.z - SHIFT
                );
            }
            return move_position;
        }

        /// <summary>
        /// move to new position.
        /// </summary>
        void moveWingmanPosition(Vector3 movePosition) {
            const float DURATION = 4.5f;
            transform.position = Vector3.Slerp(transform.position, movePosition, Time.deltaTime * DURATION);
        }

        /// <summary>
        /// returns an enum of the vehicle's direction.
        /// </summary>
        Direction getDirection(Vector3 forwardVector) {
            var forward_x = (float)Math.Round(forwardVector.x);
            var forward_y = (float)Math.Round(forwardVector.y);
            var forward_z = (float)Math.Round(forwardVector.z);
            if (forward_x == 0 && forward_z == 1) { return Direction.PositiveZ; } // z-axis positive.
            if (forward_x == 0 && forward_z == -1) { return Direction.NegativeZ; } // z-axis negative.
            if (forward_x == 1 && forward_z == 0) { return Direction.PositiveX; } // x-axis positive.
            if (forward_x == -1 && forward_z == 0) { return Direction.NegativeX; } // x-axis negative.
            // determine the difference between the two axes.
            float absolute_x = Math.Abs(forwardVector.x);
            float absolute_z = Math.Abs(forwardVector.z);
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
