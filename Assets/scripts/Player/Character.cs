using UnityEngine;

public abstract class Character
{

    public abstract float takeDamage();
    public abstract float attack();
    public abstract int spawn();
    public abstract int die();


    float health;
    float walk_speed;
    float attack_cooldown;
    float attack_power;
    float spawn_price;
}
