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
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// common processing of objects
    /// </summary>
    /// <author>
    /// h.adachi (STUDIO MeowToon)
    /// </author>
    public class Common : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        AutoDestroyParam _auto_destroy_param;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives]

        /// <summary>
        /// clear automatically after `n` seconds.
        /// </summary>
        public float autoDestroyAfter { 
            set { 
                _auto_destroy_param.enable = true; 
                _auto_destroy_param.limit = getRandomLimit(value); 
            } 
        }

        /// <summary>
        /// clear automatically after 2 seconds.
        /// </summary>
        public bool autoDestroy { 
            set { 
                _auto_destroy_param.enable = value;
                _auto_destroy_param.limit = getRandomLimit(2.0f); 
            } 
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        protected void Awake() {
            _auto_destroy_param = AutoDestroyParam.GetInstance();
        }

        // Start is called before the first frame update.
        protected void Start() {
            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
                doAutoDestroy(); // automatic deletion.
                if (transform.position.y < -100f) { // less than -100m, it has fallen from the level,
                    Destroy(gameObject); // so it is deleted.
                }
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// random number of seconds to remove automatically.
        /// </summary>
        float getRandomLimit(float limit) {
            var random = new System.Random();
            return random.Next(2, (int)limit * 25); // from 2 seconds to (limit * 25) seconds.
        }

        /// <summary>
        /// automatically delete myself.
        /// </summary>
        void doAutoDestroy() {
            if (_auto_destroy_param.enable) { // if auto delete is enabled.
                _auto_destroy_param.second += Time.deltaTime;
                if (_auto_destroy_param.second > 0.1f) { // after 0.1 seconds, collider enabled on.
                    gameObject.Get<Collider>().enabled = true;
                }
                fadeoutToDestroy(); // gradually become transparent.
                if (_auto_destroy_param.second > _auto_destroy_param.limit) { // when the limit time comes,
                    Destroy(gameObject); // delete myself.
                }
            }
        }

        /// <summary>
        /// automatic gradual transparency.
        /// </summary>
        void fadeoutToDestroy() {
            if (_auto_destroy_param.second > _auto_destroy_param.limit - 0.8f) { // from 0.8 seconds before the limit time.
                var render = gameObject.Get<MeshRenderer>();
                var materialList = render.materials;
                foreach (var material in materialList) {
                    Utils.SetRenderingMode(material, RenderingMode.Fade);
                    var color = material.color;
                    color.a = _auto_destroy_param.limit - _auto_destroy_param.second; // gradually becoming transparent.
                    material.color = color;
                }
            }
        }

        #region AutoDestroyParam

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        /// <summary>
        /// parameter class for automatic deletion.
        /// </summary>
        protected class AutoDestroyParam {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            float _second; // for adding seconds until deletion.

            float _limit; // after how many seconds it will be deleted.

            bool _enable; // auto delete activation flag.

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            AutoDestroyParam(float second, float limit, bool enable) {
                _second = second;
                _limit = limit;
                _enable = enable;
            }

            public static AutoDestroyParam GetInstance() {
                return new AutoDestroyParam(second: 0.0f, limit: 0.0f, enable: false);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public float second { get => _second; set => _second = value; }

            public float limit { get => _limit; set => _limit = value; }

            public bool enable { get => _enable; set => _enable = value; }
        }

        #endregion
    }
}
