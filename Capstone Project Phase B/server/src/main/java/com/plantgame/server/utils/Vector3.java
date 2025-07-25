package com.plantgame.server.utils;

/**
 * Represents a three-dimensional vector with x, y, and z components.
 * <p>
 * This class provides methods for accessing and modifying the x, y, and z
 * components of the vector. It can be used to represent points or directions
 * in a 3D space.
 */
public class Vector3 {
    private float x;
    private float y;
    private float z;

    public Vector3() { }

    public Vector3(float x, float y, float z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public float getX() {
        return x;
    }

    public void setX(float x) {
        this.x = x;
    }

    public float getY() {
        return y;
    }

    public void setY(float y) {
        this.y = y;
    }

    public float getZ() {
        return z;
    }

    public void setZ(float z) {
        this.z = z;
    }
}
