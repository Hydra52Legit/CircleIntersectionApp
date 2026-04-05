using System;

namespace CircleIntersectionApp.Models;

public class CircleData
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double R1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double R2 { get; set; }

    public bool CirclesIntersect()
    {
        double dx = X2 - X1;
        double dy = Y2 - Y1;
        double d = Math.Sqrt(dx * dx + dy * dy);
        double sumRadii = R1 + R2;
        double diffRadii = Math.Abs(R1 - R2);

        return d < sumRadii && d > diffRadii;
    }

    public (double x1, double y1, double x2, double y2)? GetIntersectionPoints()
    {
        if (!CirclesIntersect())
            return null;

        double dx = X2 - X1;
        double dy = Y2 - Y1;
        double d = Math.Sqrt(dx * dx + dy * dy);

        if (d == 0.0)
            return null;

        double a = (R1 * R1 - R2 * R2 + d * d) / (2 * d);
        double h = Math.Sqrt(Math.Max(0.0, R1 * R1 - a * a));

        double xm = X1 + (dx * a) / d;
        double ym = Y1 + (dy * a) / d;

        double xs1 = xm + h * dy / d;
        double ys1 = ym - h * dx / d;
        double xs2 = xm - h * dy / d;
        double ys2 = ym + h * dx / d;

        return (xs1, ys1, xs2, ys2);
    }
}
