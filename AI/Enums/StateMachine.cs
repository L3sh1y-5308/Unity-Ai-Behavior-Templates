using System;
using Unity.Behavior;

[BlackboardEnum]
public enum StateMachine
{
	Idle,
	Hunger,
	Hunting,
	ChasePlayer,
	Attak,
	ScaredRun,
	Patrol
}
