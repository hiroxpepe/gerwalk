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
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;

namespace Studio.MeowToon {
    /// <summary>
    /// game system
    /// @author h.adachi
    /// </summary>
    public class StatusSystem : MonoBehaviour {
#nullable enable

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        const string TARGETS_OBJECT = "Balloons"; // name of target objects holder.

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Text _message_text;

        [SerializeField]
        Text _targets_text;

        [SerializeField]
        Text _speed_text;

        [SerializeField]
        Text _altitude_text;

        [SerializeField]
        Text _debug_text;

        [SerializeField]
        GameObject _player;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        int _target_total_count = 0;

        int _target_remain_count = 0;

        float _speed = 0f;

        float _altitude = 0f;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        void Start() {
            // get targets count.
            _target_total_count = getTargetsCount();
            // update text ui.
            this.UpdateAsObservable().Subscribe(_ => {
                checkGameStatus();
                updateGameStatus();
                updatePlayerStatus();
            });
            // get player speed.
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                var player_controller = _player.gameObject.GetPlayerController();
                _speed = player_controller.flySpeed;
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// check game status
        /// </summary>
        void checkGameStatus() {
            _target_remain_count = getTargetsCount();
        }

        /// <summary>
        /// update game status
        /// </summary>
        void updateGameStatus() {
            _targets_text.text = string.Format("TGT {0}/{1}", _target_total_count - _target_remain_count, _target_total_count);
        }

        /// <summary>
        /// update player status
        /// </summary>
        void updatePlayerStatus() {
            _speed_text.text = string.Format("TAS {0:000.0}km", Math.Round(_speed, 1, MidpointRounding.AwayFromZero));
            _altitude = _player.transform.position.y - 0.5f; // 0.5 is half player height.
            _altitude_text.text = string.Format("ALT {0:000.0}m", Math.Round(_altitude, 1, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// get targets count.
        /// </summary>
        int getTargetsCount() {
            var targets = GameObject.Find(TARGETS_OBJECT);
            Transform targets_transform = targets.GetComponentInChildren<Transform>();
            return targets_transform.childCount;
        }
        
        /// <summary>
        /// debug trace
        /// </summary>
        public void trace(string value) {
            _debug_text.text = value;
        }
    }
}
