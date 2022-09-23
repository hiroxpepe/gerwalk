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

using System.Linq;
using UnityEngine;
using static UnityEngine.Vector3;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// camera controller.
    /// @author h.adachi
    /// </summary>
    public class CameraSystem : GamepadMaper {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References

        [SerializeField]
        GameObject _horizontal_axis;

        [SerializeField]
        GameObject _vertical_axis;

        [SerializeField]
        GameObject _main_camera;

        [SerializeField]
        GameObject _look_target;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        Vector3 _default_local_position;

        Quaternion _default_local_rotation;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        new void Start() {
            base.Start();

            /// <summary>
            /// hold the default position and rotation of the camera.
            /// </summary>
            _default_local_position = transform.localPosition;
            _default_local_rotation = transform.localRotation;

            /// <summary>
            /// rotate the camera view.
            /// </summary>
            this.UpdateAsObservable().Subscribe(_ => {
                rotateView();
            });

            /// <summary>
            /// reset the camera view.
            /// </summary>
            this.UpdateAsObservable().Where(_ => _right_stick_button.wasPressedThisFrame).Subscribe(_ => {
                resetRotateView();
            });

            /// <summary>
            /// when touching the back wall.
            /// </summary>
            this.OnTriggerEnterAsObservable().Where(x => x.LikeWall()).Subscribe(x => {
                var material_list = x.gameObject.GetMeshRenderer().materials.ToList();
                material_list.ForEach(material => {
                    material.ToOpaque();
                });
            });

            /// <summary>
            /// when leaving the back wall.
            /// </summary>
            this.OnTriggerExitAsObservable().Where(x => x.LikeWall()).Subscribe(x => {
                var material_list = x.gameObject.GetMeshRenderer().materials.ToList();
                material_list.ForEach(material => {
                    material.ToTransparent();
                });
            });

            /// <summary>
            /// when touching the block.
            /// </summary>
            this.OnTriggerEnterAsObservable().Where(x => x.LikeBlock()).Subscribe(x => {
                var material_list = x.gameObject.GetMeshRenderer().materials.ToList();
                material_list.ForEach(material => {
                    material.ToOpaque();
                });
            });

            /// <summary>
            /// when leaving the block.
            /// </summary>
            this.OnTriggerExitAsObservable().Where(x => x.LikeBlock()).Subscribe(x => {
                var material_list = x.gameObject.GetMeshRenderer().materials.ToList();
                material_list.ForEach(material => {
                    material.ToTransparent();
                });
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// rotate the camera view.
        /// </summary>
        void rotateView() {
            const float ADJUST = 120.0f;
            var player_position = transform.parent.gameObject.transform.position;
            // up.
            if (_right_stick_up_button.isPressed) {
                transform.RotateAround(player_position, right, 1.0f * ADJUST * Time.deltaTime);
                transform.LookAt(_look_target.transform);
            }
            // down.
            else if (_right_stick_down_button.isPressed) {
                transform.RotateAround(player_position, right, -1.0f * ADJUST * Time.deltaTime);
                transform.LookAt(_look_target.transform);
            }
            // left.
            else if (_right_stick_left_button.isPressed) {
                transform.RotateAround(player_position, up, 1.0f * ADJUST * Time.deltaTime);
            }
            // right
            else if (_right_stick_right_button.isPressed) {
                transform.RotateAround(player_position, up, -1.0f * ADJUST * Time.deltaTime);
            }
        }

        /// <summary>
        /// reset the camera view.
        /// </summary>
        void resetRotateView() {
            transform.localPosition = _default_local_position;
            transform.localRotation = _default_local_rotation;
        }
    }
}
