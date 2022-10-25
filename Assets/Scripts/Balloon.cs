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
using static UnityEngine.GameObject;
using UniRx;
using UniRx.Triggers;

using static Studio.MeowToon.Env;

namespace Studio.MeowToon {
    /// <summary>
    /// balloon class
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Balloon : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField] GameObject _item_object;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields [noun, adjectives] 

        System.Random _random;

        /// <summary>
        /// will be destroyed by the next attack.
        /// </summary>
        bool _destroyable = true;

        ExplodeParam _explode_param;

        DoFixedUpdate _do_fixed_update;

        GameSystem _game_system;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Awake is called when the script instance is being loaded.
        void Awake() {
            _random = new();
            _do_fixed_update = DoFixedUpdate.getInstance();
            _explode_param = ExplodeParam.getDefaultInstance();
            _game_system = Find(name: GAME_SYSTEM).Get<GameSystem>();
            initializePiece();
        }

        // Start is called before the first frame update.
        void Start() {
            // FixedUpdate is called just before each physics update.
            this.FixedUpdateAsObservable().Where(predicate: _ => _do_fixed_update.explode).Subscribe(onNext: _ => {
                gameObject.Get<SphereCollider>().enabled = false; // collider detection off. *passed on to children.
                explodePiece();
                gameObject.Get<SphereCollider>().enabled = true; // collider detection on.
                _do_fixed_update.explode = false;
                if (_destroyable) {
                    Destroy(gameObject); // delete myself
                }
            });

            /// <summary>
            /// wwhen being touched vehicle.
            /// </summary>
            this.OnCollisionEnterAsObservable().Where(predicate: x => x.Like(VEHICLE_TYPE)).Subscribe(onNext: x => {
                destroyWithItems(x.transform);
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// scatter items before destroy.
        /// </summary>
        void destroyWithItems(Transform bullet, int number_of_piece = 8) {
            if (_destroyable) {
                _do_fixed_update.explode = true;
            }
        }

        /// <summary>
        /// initialize pieces.
        /// </summary>
        void initializePiece() {
            int number = _explode_param.number;
            float scale = _explode_param.scale;
            for (int i = 0; i < number; i++) {
                GameObject piece = Instantiate(original: _item_object);
                Coin coin = piece.Get<Coin>();
                coin.OnDestroy += () => {
                    _game_system.IncrementPoints();
                };
                Vector3 position = transform.position;
                int shift = i % 4;
                piece.transform.position = new Vector3( // set shifted position.
                    x: position.x + ((float) shift / 2.25f) - 0.65f,
                    y: position.y + ((float) shift / 1.0f) - 2.00f,
                    z: position.z + ((float) shift / 2.25f) - 0.65f
                );
                piece.name += "_Piece"; // add "_Piece" to the name of the piece.
                piece.transform.localScale = new Vector3(x: scale, y: scale, z: scale);
                if (piece.Get<Rigidbody>() == null) {
                    piece.Add<Rigidbody>();
                }
                piece.Get<Rigidbody>().useGravity = false;
                piece.Get<Rigidbody>().isKinematic = true;
                piece.transform.parent = transform;
            }
        }

        /// <summary>
        /// explode pieces.
        /// </summary>
        void explodePiece() {
            int force = _explode_param.force;
            float scale = _explode_param.scale;
            foreach (Transform piece in transform) {
                piece.parent = null;
                piece.transform.localScale = new Vector3(x: scale * 2, y: scale * 2, z: scale * 2);
                int min = -getRandomForce(force);
                int max = getRandomForce(force);
                Vector3 force_value = new Vector3(x: _random.Next(min, max), y: _random.Next(min, max), z: _random.Next(min, max));
                piece.Get<Rigidbody>().useGravity = true;
                piece.Get<Rigidbody>().isKinematic = false;
                piece.Get<Rigidbody>().AddTorque(torque: force_value, mode: ForceMode.Impulse);
                piece.Get<Rigidbody>().AddForce(force: force_value, mode: ForceMode.Impulse);
                piece.gameObject.Get<Coin>().autoDestroy = true; // clear pieces after 2 seconds.
            }
        }

        /// <summary>
        /// get a random value for the force applied to the flying pieces.
        /// </summary>
        int getRandomForce(int force) {
            return _random.Next(minValue: (int) force / 2, maxValue: (int) force * 2); // range of 1/2 to 2 times the force.
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
                DoFixedUpdate instance = new();
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
