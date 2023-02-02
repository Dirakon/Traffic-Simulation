namespace TrafficSimulation;

public class MathUtils
{
    public static int Mod(int x, int m) {
        int r = x%m;
        return r<0 ? r+m : r;
    }
    public static double Mod(double x, double m) {
        double r = x%m;
        return r<0 ? r+m : r;
    }
    public static float Mod(float x, float m) {
        float r = x%m;
        return r<0 ? r+m : r;
    }


}