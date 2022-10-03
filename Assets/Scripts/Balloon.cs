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

using System.Linq;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// balloon controller.
    /// @author h.adachi
    /// </summary>
    public class Balloon : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        GameObject _item_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        /// <summary>
        /// will be destroyed by the next attack.
        /// </summary>
        bool _destroyable = true;

        ExplodeParam _explode_param;

        DoFixedUpdate _do_fixed_update;

        StatusSystem _status_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _do_fixed_update = DoFixedUpdate.getInstance();
            _explode_param = ExplodeParam.getDefaultInstance();
            _status_system = gameObject.GetStatusSystem();
            initializePiece();
        }

        // Start is called before the first frame update.
        void Start() {
            // Update is called once per frame.
            this.UpdateAsObservable().Subscribe(_ => {
            });

            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable().Where(_ => _do_fixed_update.explode).Subscribe(_ => {
                gameObject.GetSphereCollider().enabled = false; // collider detection off. *passed on to children.
                explodePiece();
                gameObject.GetSphereCollider().enabled = true; // collider detection on.
                _do_fixed_update.explode = false;
                if (_destroyable) {
                    Destroy(gameObject); // delete myself
                }
            });

            /// <summary>
            /// wwhen being touched vehicle.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(x => x.LikeVehicle()).Subscribe(x => {
                destroyWithItems(x.transform);
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// scatter items before destroy.
        /// </summary>
        void destroyWithItems(Transform bullet, int numberOfPiece = 8) {
            if (_destroyable) {
                _do_fixed_update.explode = true;
            }
        }

        /// <summary>
        /// initialize pieces.
        /// </summary>
        void initializePiece() {
            var number = _explode_param.number;
            var scale = _explode_param.scale;
            for (var i = 0; i < number; i++) {
                var piece = Instantiate(_item_object);
                var coin = piece.GetCoin();
                coin.OnDestroy += () => {
                    _status_system.IncrementPoints();
                };
                var position = transform.position;
                var shift = i % 4;
                piece.transform.position = new Vector3( // set shifted position.
                    position.x + ((float) shift / 2.25f) - 0.65f,
                    position.y + ((float) shift / 1.0f) - 2.00f,
                    position.z + ((float) shift / 2.25f) - 0.65f
                );
                piece.name += "_Piece"; // add "_Piece" to the name of the piece.
                piece.transform.localScale = new Vector3(scale, scale, scale);
                if (piece.GetRigidbody() == null) {
                    piece.AddRigidbody();
                }
                piece.GetRigidbody().useGravity = false;
                piece.GetRigidbody().isKinematic = true;
                piece.transform.parent = transform;
            }
        }

        /// <summary>
        /// explode pieces.
        /// </summary>
        void explodePiece() {
            var force = _explode_param.force;
            var scale = _explode_param.scale;
            foreach (Transform piece in transform) {
                piece.parent = null;
                piece.transform.localScale = new Vector3(scale * 2, scale * 2, scale * 2);
                var random = new System.Random();
                var min = -getRandomForce(force);
                var max = getRandomForce(force);
                var force_value = new Vector3(random.Next(min, max), random.Next(min, max), random.Next(min, max));
                piece.GetRigidbody().useGravity = true;
                piece.GetRigidbody().isKinematic = false;
                piece.GetRigidbody().AddTorque(force_value, ForceMode.Impulse);
                piece.GetRigidbody().AddForce(force_value, ForceMode.Impulse);
                piece.gameObject.GetCoin().autoDestroy = true; // clear pieces after 2 seconds.
            }
        }

        /// <summary>
        /// get a random value for the force applied to the flying pieces.
        /// </summary>
        int getRandomForce(int force) {
            var random = new System.Random();
            return random.Next((int) force / 2, (int) force * 2); // range of 1/2 to 2 times the force.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // inner Classes

        #region DoFixedUpdate

        /// <summary>
        /// class for FixedUpdate() method.
        /// </summary>
        protected class DoFixedUpdate {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            bool _explode;

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public bool explode { get => _explode; set => _explode = value; }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            /// <summary>
            /// returns an initialized instance.
            /// </summary>
            public static DoFixedUpdate getInstance() {
                var instance = new DoFixedUpdate();
                instance.ResetMotion();
                return instance;
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // public Methods [verb]

            /// <summary>
            /// Initialize all fields.
            /// </summary>
            public void ResetMotion() {
                _explode = false;
            }
        }

        #endregion

        #region ExplodeParam

        /// <summary>
        /// parameter class for item generation.
        /// </summary>
        protected class ExplodeParam {

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Fields [noun, adjectives] 

            int _number; // number of pieces.

            float _scale; // scale of pieces.

            int _force; // force of to scat pieces.

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Constructor

            ExplodeParam(int number, float scale, int force) {
                _number = number;
                _scale = scale;
                _force = force;
            }

            public static ExplodeParam getDefaultInstance() {
                return new ExplodeParam(number: 32, scale: 1.0f, force: 10); // default value.
            }

            public static ExplodeParam getInstance(int number, float scale, int force) {
                return new ExplodeParam(number, scale, force);
            }

            ///////////////////////////////////////////////////////////////////////////////////////////
            // Properties [noun, adjectives] 

            public int number { get => _number; }

            public float scale { get => _scale; }

            public int force { get => _force; }
        }

        #endregion
    }
}
