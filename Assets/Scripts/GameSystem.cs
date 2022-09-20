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
    public class GameSystem : GamepadMaper {

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Constants

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // References [bool => is+adjective, has+past participle, can+verb prototype, triad verb]

        [SerializeField]
        Text _message;

        [SerializeField]
        Text _targets;

        [SerializeField]
        Text _speed;

        [SerializeField]
        Text _altitude;

        [SerializeField]
        GameObject _player;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Fields

        int _balloonTotalCount = 0;

        int _balloonRemainCount = 0;

        float _speedValue = 0f;

        float _altitudeValue = 0f;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Properties [noun, adjectives] 

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public Methods [verb]

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // update Methods

        // Start is called before the first frame update
        new void Start() {
            base.Start();
            // get targets count.
            var target = GameObject.Find("Balloons");
            Transform balloons = target.GetComponentInChildren<Transform>();
            _balloonTotalCount = balloons.childCount;
            // update text ui.
            this.UpdateAsObservable().Subscribe(_ => {
                checkGameStatus();
                updateGameStatus();
                updatePlayerStatus();
            });
            // get player speed.
            Vector3 prevPosition = _player.transform.position;
            this.FixedUpdateAsObservable().Where(_ => !Mathf.Approximately(Time.deltaTime, 0)).Subscribe(_ => {
                _speedValue = ((_player.transform.position - prevPosition) / Time.deltaTime).magnitude * 3.6f;
                prevPosition = _player.transform.position;
            });
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        /// <summary>
        /// check game status
        /// </summary>
        void checkGameStatus() {
            var target = GameObject.Find("Balloons");
            Transform balloons = target.GetComponentInChildren<Transform>();
            _balloonRemainCount = balloons.childCount;
        }

        /// <summary>
        /// update game status
        /// </summary>
        void updateGameStatus() {
            _targets.text = string.Format("TGT {0}/{1}", _balloonTotalCount - _balloonRemainCount, _balloonTotalCount);
        }

        /// <summary>
        /// update player status
        /// </summary>
        void updatePlayerStatus() {
            _speed.text = string.Format("TAS {0:000.0}km", Math.Round(_speedValue, 1, MidpointRounding.AwayFromZero));
            _altitudeValue = _player.transform.position.y - 0.5f; // 0.5 is half player height.
            _altitude.text = string.Format("ALT {0:000.0}m", Math.Round(_altitudeValue, 1, MidpointRounding.AwayFromZero));
        }
    }
}
