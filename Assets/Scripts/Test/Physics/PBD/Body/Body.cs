using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PositionBasedHighlight
{
    #region Struct_Definition
    public struct ParticleData
    {
        public Vector2 position;
        public Vector2 predicted;
        public Vector2 velocity;

        public ParticleData(Vector2 pos)
        {
            position = pos;
            predicted = pos;
            velocity = new Vector2(0, 0);
        }
    }

    public struct ObjectToParticles
    {
        public int pStart;
        public int pCount;

        public ObjectToParticles(int pStart, int pCount)
        {
            this.pStart = pStart;
            this.pCount = pCount;
        }
    }
    #endregion

    public class Body
    {
        public ComputeBuffer ParticleBuffer { get; private set; }

        public ComputeBuffer LocalPosBuffer { get; private set; }
        public ComputeBuffer ObjectIndexBuffer { get; private set; }
        public ComputeBuffer LayerBuffer { get; private set; }

        public DistanceConstraint DistanceConstraint { get; private set; }
        public AreaConstraint AreaConstraint { get; private set; }
        public ShapeMatchConstraint ShapeMatchConstraint { get; private set; }

        public CollisionSolver CollisionSolver { get; private set; }
        public TargetPosSolver TargetPosSolver { get; private set; }
        public ParticleCollisionSolver ParticleCollisionSolver { get; private set; }

        public ObjectToParticles[] ObjToParticles { get; private set; }

        public Body(
            ComputeBuffer particles, ComputeBuffer localPositions, ComputeBuffer objIndices, ComputeBuffer layers,
            DistanceConstraint distConst, AreaConstraint areaConst, ShapeMatchConstraint shapeMatchConst,
            CollisionSolver collisionConst, TargetPosSolver targetPosConst, ParticleCollisionSolver particleCollisionConst,
            ObjectToParticles[] references)
        {
            ParticleBuffer = particles;
            LocalPosBuffer = localPositions;
            ObjectIndexBuffer = objIndices;
            LayerBuffer = layers;

            DistanceConstraint = distConst;
            AreaConstraint = areaConst;
            ShapeMatchConstraint = shapeMatchConst;

            CollisionSolver = collisionConst;
            TargetPosSolver = targetPosConst;
            ParticleCollisionSolver = particleCollisionConst;

            ObjToParticles = references;
        }

        public void ApplyExternalData(ExternalDataPool dataPool)
        {
            CollisionSolver.SetSDFArray(dataPool.SDFArray);
            CollisionSolver.SetColliderTransforms(dataPool.ColliderTransforms);

            TargetPosSolver.SetOffsets(dataPool.TargetPosTransforms);
        }

        public void ReleaseBuffers()
        {
            ComputeHelper.Release(
                ParticleBuffer,
                LocalPosBuffer,
                ObjectIndexBuffer,
                LayerBuffer
                );

            if (DistanceConstraint != null) DistanceConstraint.Release();
            if (AreaConstraint != null) AreaConstraint.Release();
            if (ShapeMatchConstraint != null) ShapeMatchConstraint.Release();

            if (TargetPosSolver != null) TargetPosSolver.ReleaseBuffers();
            if (CollisionSolver != null) CollisionSolver.ReleaseBuffers();
            if (ParticleCollisionSolver != null) ParticleCollisionSolver.ReleaseBuffers();
        }

    }
}
