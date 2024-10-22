using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace BXRenderPipeline
{
    public class BXVolumeDrawer
    {
        static readonly Dictionary<Type, IVolumeAdditionalGizmo> s_AdditionalGizmoCallbacks = new ();

        [InitializeOnLoadMethod]
        static void InitVolumeGizmosCallbacs()
		{
            foreach(var additionalGizmosCallback in TypeCache.GetTypesDerivedFrom<IVolumeAdditionalGizmo>())
			{
                if (additionalGizmosCallback.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
                    continue;

                var instance = Activator.CreateInstance(additionalGizmosCallback) as IVolumeAdditionalGizmo;
                s_AdditionalGizmoCallbacks.Add(instance.type, instance);
            }
		}

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        static void onDrawGizmos(IVolume src, GizmoType gizmoType)
		{
            if (src is not MonoBehaviour monoBehaviour)
                return;

            if (!monoBehaviour.enabled)
                return;

            if (src.isGlobal || src.colliders == null)
                return;

            var lossyScale = monoBehaviour.transform.lossyScale;
            Gizmos.matrix = Matrix4x4.TRS(monoBehaviour.transform.position, monoBehaviour.transform.rotation, lossyScale);

            var gizmoColor = VolumesPreferences.volumeGizmoColor;
            var gizmoColorWhenBlendRegionEnable = new Color(gizmoColor.r, gizmoColor.r, gizmoColor.b, 0.5f);

            s_AdditionalGizmoCallbacks.TryGetValue(src.GetType(), out var callback);

            foreach(var collider in src.colliders)
			{
                if (!collider || !collider.enabled)
                    continue;

                float blendDistance = 0f;
                if (src is BXRenderSettingsVolume volume)
                    blendDistance = volume.blendDistance;

                bool blendDistanceEnable = blendDistance > 0f;
                Gizmos.color = blendDistanceEnable && VolumesPreferences.drawSolid ? gizmoColorWhenBlendRegionEnable : gizmoColor;

				switch (collider)
				{
                    case BoxCollider c: DrawBoxCollider(c); break;
                    case SphereCollider c: DrawSphereCollider(c); break;
                    case MeshCollider c: DrawMeshCollider(c); break;
                    default:
                        break;
				}

                void DrawBoxCollider(BoxCollider boxCollider)
				{
                    if (VolumesPreferences.drawWireFrame)
                        Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                    if (VolumesPreferences.drawSolid)
                        Gizmos.DrawCube(boxCollider.center, boxCollider.size);

                    if (blendDistanceEnable)
                        DrawBlendDistanceBox(boxCollider, blendDistance);

                    callback?.OnBoxColliderDraw(src, boxCollider);
				}

                void DrawSphereCollider(SphereCollider c)
				{
                    Matrix4x4 oldMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(monoBehaviour.transform.position, monoBehaviour.transform.rotation, Vector3.one * lossyScale.x);

                    if (VolumesPreferences.drawWireFrame)
                        Gizmos.DrawWireSphere(c.center, c.radius);
                    if (VolumesPreferences.drawSolid)
                        Gizmos.DrawSphere(c.center, c.radius);

                    if (blendDistanceEnable)
                        DrawBlendDistanceSphere(c, blendDistance);

                    callback?.OnSphereColliderDraw(src, c);

                    Gizmos.matrix = oldMatrix;
                }

                void DrawMeshCollider(MeshCollider c)
				{
                    if (!c.convex)
                        c.convex = true;

                    if (VolumesPreferences.drawWireFrame)
                        Gizmos.DrawWireMesh(c.sharedMesh);
                    if (VolumesPreferences.drawSolid)
                        Gizmos.DrawMesh(c.sharedMesh);

                    callback?.OnMeshColliderDraw(src, c);
                }
            }
		}

        static void DrawBlendDistanceBox(BoxCollider c, float blendDistance)
		{
            var twiceFadeRadius = blendDistance * 2f;
            var transformScale = c.transform.localScale;
            Vector3 size = c.size + new Vector3
            (
                twiceFadeRadius / transformScale.x,
                twiceFadeRadius / transformScale.y,
                twiceFadeRadius / transformScale.z
            );

            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireCube(c.center, size);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawCube(c.center, size);
		}

        static void DrawBlendDistanceSphere(SphereCollider c, float blendDistance)
		{
            var blendSphereSize = c.radius + blendDistance / c.transform.lossyScale.x;
            if (VolumesPreferences.drawWireFrame)
                Gizmos.DrawWireSphere(c.center, blendSphereSize);
            if (VolumesPreferences.drawSolid)
                Gizmos.DrawSphere(c.center, blendSphereSize);
		}
    }
}
