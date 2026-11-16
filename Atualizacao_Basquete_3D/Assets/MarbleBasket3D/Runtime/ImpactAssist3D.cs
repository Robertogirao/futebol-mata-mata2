using MarbleBasket3D.Core;
using UnityEngine;

namespace MarbleBasket3D
{
    // Called only on the first physical wheel collision of an attempt.
    // It changes velocity once, never position, team, score or the later flight.
    public static class ImpactAssist3D
    {
        public static Vector3 Velocity(Vector3 position, Vector3 target, Vector3 physicalVelocity, ArenaSettings3D s, float now)
        {
            if (!s.impactAssist || s.impactAssistStrength <= 0 || physicalVelocity.sqrMagnitude < 1) return physicalVelocity;
            Vector3 heading = target - position; heading.y = 0;
            if (heading.sqrMagnitude < .01f || Physics.gravity.y >= 0) return physicalVelocity;
            heading.Normalize();
            Vector3 sideways = Vector3.Cross(Vector3.up, heading);
            float cycle = now * Mathf.PI * 2 / s.powerCycleSeconds;
            Vector3 aim = target + heading * (Mathf.Sin(cycle) * s.impactTargetSpread)
                + sideways * (Mathf.Sin(cycle * 1.37f + .8f) * s.impactTargetSpread);
            // The actual contact leaves a bounded imprint on the spread, without selecting a winner.
            Vector3 incoming = physicalVelocity; incoming.y = 0;
            aim += incoming / Mathf.Max(1, incoming.magnitude) * (s.impactTargetSpread * .12f);
            Vector3 delta = aim - position; delta.y = 0;
            float angle = s.launchAngle + Mathf.Sin(cycle * .83f) * s.angleVariation;
            float radians = angle * Mathf.Deg2Rad;
            if (delta.magnitude * Mathf.Tan(radians) <= aim.y - position.y) return physicalVelocity;
            float speed = (float)LaunchMath3D.RequiredSpeed(delta.magnitude, aim.y - position.y, angle, -Physics.gravity.y)
                + Mathf.Sin(cycle + 1.1f) * s.speedVariation;
            Vector3 direction = Quaternion.Euler(0, Mathf.Sin(cycle * .73f) * s.launchYawVariation, 0) * delta.normalized;
            Vector3 desired = (direction * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians)) * speed;
            Vector3 correction = (desired - physicalVelocity) * s.impactAssistStrength;
            float limit = physicalVelocity.magnitude * s.impactAssistStrength;
            correction *= Mathf.Min(1, limit / Mathf.Max(.0001f, correction.magnitude));
            return physicalVelocity + correction;
        }
    }
}
