using System;

namespace MarbleBasket3D.Core
{
    // One scoring opportunity per launch, rearmed only by a new launch.
    public sealed class ShotValidation3D
    {
        public int Attempt { get; private set; }
        public bool Scored { get; private set; }
        public bool Armed { get; private set; }
        private bool sampled, pendingCrossing;
        private double px, py, pz;
        public void Reset() { Attempt = 0; Scored = Armed = sampled = pendingCrossing = false; }
        public void Launch() { Attempt++; Scored = Armed = sampled = pendingCrossing = false; }
        public bool Sample(double x, double y, double z, double rimX, double rimY, double rimZ, double clearance, double ballRadius)
        {
            if (Scored || Attempt == 0) return false;
            if (y >= rimY + ballRadius) Armed = true;
            bool crossed = false;
            if (sampled && Armed && py >= rimY && y < rimY && py > y && clearance > ballRadius)
            {
                double t = (py - rimY) / (py - y);
                double dx = px + (x - px) * t - rimX, dz = pz + (z - pz) * t - rimZ;
                double r = clearance - ballRadius;
                pendingCrossing = dx * dx + dz * dz < r * r;
            }
            // The centre crossing is only a candidate. Confirm after the whole sphere
            // passes below the rim, and cancel a bounce back above the opening.
            if (pendingCrossing && y > rimY) pendingCrossing = false;
            double lower = rimY - ballRadius;
            if (sampled && pendingCrossing && py > lower && y <= lower && py > y)
            {
                double t = (py - lower) / (py - y);
                double dx = px + (x - px) * t - rimX, dz = pz + (z - pz) * t - rimZ;
                crossed = dx * dx + dz * dz < clearance * clearance;
                pendingCrossing = false;
            }
            px = x; py = y; pz = z; sampled = true;
            if (crossed) Scored = true;
            return crossed;
        }
    }

    public static class LaunchMath3D
    {
        public static double RequiredSpeed(double horizontalDistance, double heightDifference, double angleDegrees, double gravity)
        {
            double angle = angleDegrees * Math.PI / 180;
            double denominator = 2 * Math.Cos(angle) * Math.Cos(angle) * (horizontalDistance * Math.Tan(angle) - heightDifference);
            if (horizontalDistance <= 0 || gravity <= 0 || denominator <= 0) throw new ArgumentException("Target cannot be reached at this angle.");
            return Math.Sqrt(gravity * horizontalDistance * horizontalDistance / denominator);
        }
    }
}
