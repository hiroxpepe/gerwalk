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

using System.Collections.Generic;
using UnityEngine;

namespace Studio.MeowToon {
    /// <summary>
    /// generic extension method.
    /// @author h.adachi
    /// </summary>
    public static class Extensions {
#nullable enable

        #region type of object.

        /// <summary>
        /// whether the GameObject's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this GameObject self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Transform's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Transform self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Collider's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Collider self) {
            return self.name.Contains("Block");
        }

        /// <summary>
        /// whether the Collision's name contains "Block".
        /// </summary>
        public static bool LikeBlock(this Collision self) {
            return self.gameObject.name.Contains("Block");
        }

        /// <summary>
        /// whether the GameObject's name contains "Ground".
        /// </summary>
        public static bool LikeGround(this GameObject self) {
            return self.name.Contains("Ground");
        }

        /// <summary>
        /// whether the Transform's name contains "Ground".
        /// </summary>
        public static bool LikeGround(this Transform self) {
            return self.name.Contains("Ground");
        }

        /// <summary>
        /// whether the Collision's name contains "Ground".
        /// </summary>
        public static bool LikeGround(this Collision self) {
            return self.gameObject.name.Contains("Ground");
        }

        /// <summary>
        /// whether the GameObject's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this GameObject self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Transform's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Transform self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collider's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Collider self) {
            return self.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collision's name contains "Wall".
        /// </summary>
        public static bool LikeWall(this Collision self) {
            return self.gameObject.name.Contains("Wall");
        }

        /// <summary>
        /// whether the Collision's name contains "Balloon".
        /// </summary>
        public static bool LikeBalloon(this Collision self) {
            return self.gameObject.name.Contains("Balloon");
        }

        /// <summary>
        /// whether the Collision's name contains "Coin".
        /// </summary>
        public static bool LikeCoin(this Collision self) {
            return self.gameObject.name.Contains("Coin");
        }

        #endregion

        #region get the object.

        /// <summary>
        /// get the Collider object.
        /// </summary>
        public static Collider GetCollider(this GameObject self) {
            return self.GetComponent<Collider>();
        }

        /// <summary>
        /// get the BoxCollider object.
        /// </summary>
        public static BoxCollider GetBoxCollider(this GameObject self) {
            return self.GetComponent<BoxCollider>();
        }

        /// <summary>
        /// get the CapsuleCollider object.
        /// </summary>
        public static CapsuleCollider GetCapsuleCollider(this GameObject self) {
            return self.GetComponent<CapsuleCollider>();
        }

        /// <summary>
        /// get the SphereCollider object.
        /// </summary>
        public static SphereCollider GetSphereCollider(this GameObject self) {
            return self.GetComponent<SphereCollider>();
        }

        /// <summary>
        /// get the Rigidbody object.
        /// </summary>
        public static Rigidbody GetRigidbody(this GameObject self) {
            return self.GetComponent<Rigidbody>();
        }

        /// <summary>
        /// get the Rigidbody object.
        /// </summary>
        public static Rigidbody GetRigidbody(this Transform self) {
            return self.GetComponent<Rigidbody>();
        }

        /// <summary>
        /// add a Rigidbody object.
        /// </summary>
        public static Rigidbody AddRigidbody(this GameObject self) {
            return self.AddComponent<Rigidbody>();
        }

        /// <summary>
        /// add a Rigidbody object.
        /// </summary>
        public static Rigidbody AddRigidbody(this Transform self) {
            return self.gameObject.AddComponent<Rigidbody>();
        }

        /// <summary>
        /// get the Renderer object.
        /// </summary>
        public static Renderer GetRenderer(this GameObject self) {
            return self.GetComponent<Renderer>();
        }

        /// <summary>
        /// get the MeshRenderer object.
        /// </summary>
        public static MeshRenderer GetMeshRenderer(this GameObject self) {
            return self.GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// get the RectTransform object.
        /// </summary>
        public static RectTransform GetRectTransform(this GameObject self) {
            return self.GetComponent<RectTransform>();
        }

        /// <summary>
        /// get Transform objects.
        /// </summary>
        public static IEnumerable<Transform> GetTransformsInChildren(this GameObject self) {
            return self.GetComponentsInChildren<Transform>();
        }

        /// <summary>
        /// get CameraSystem component.
        /// </summary>
        public static CameraSystem GetCameraSystem(this GameObject self) {
            return GameObject.Find("CameraSystem").GetComponent<CameraSystem>();
        }

        /// <summary>
        /// get StatusSystem component.
        /// </summary>
        public static StatusSystem GetStatusSystem(this GameObject self) {
            return GameObject.Find("StatusSystem").GetComponent<StatusSystem>();
        }

        /// <summary>
        /// get the Player component.
        /// </summary>
        public static Player GetPlayer(this GameObject self) {
            return self.GetComponent<Player>();
        }

        /// <summary>
        /// get the Balloon component.
        /// </summary>
        public static Balloon GetBalloon(this GameObject self) {
            return self.GetComponent<Balloon>();
        }

        /// <summary>
        /// get the Coin component.
        /// </summary>
        public static Coin GetCoin(this GameObject self) {
            return self.GetComponent<Coin>();
        }

        #endregion

        #region for Material.

        /// <summary>
        /// set Material color to opaque.
        /// </summary>
        public static Material ToOpaque(this Material self, float time = 0) {
            var color = self.color;
            color.a = 0; // to opaque.
            self.color = color;
            return self;
        }

        /// <summary>
        /// set Material color to transparent.
        /// </summary>
        public static Material ToTransparent(this Material self, float time = 0) {
            var color = self.color;
            color.a = 1; // to transparent.
            self.color = color;
            return self;
        }

        #endregion
    }
}
