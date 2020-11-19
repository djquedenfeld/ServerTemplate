using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ServerTest
{
    [Serializable]
    class Player
    {
        public int id;
        public string username;
        public Vector3 position;
        public Quaternion rotation;

        private float moveSpeed = 5f / Constants.TICKS_PER_SEC;
        private bool[] inputs;

        public Player(int _id, string _username, Vector3 _spawnPos)
        {
            id = _id;
            username = _username;
            position = _spawnPos;
            rotation = Quaternion.Identity;

            inputs = new bool[4];
        }
        public Player(int _id, string _username, Vector3 _spawnPos, Quaternion _rotation)
        {
            id = _id;
            username = _username;
            position = _spawnPos;
            rotation = _rotation;

            inputs = new bool[4];
        }

        public void Update()
        {
            Vector2 inputDir = Vector2.Zero;
            if(inputs[0])
            {
                inputDir.Y += 1;
            }
            if (inputs[1])
            {
                inputDir.Y -= 1;
            }
            if (inputs[2])
            {
                inputDir.X += 1;
            }
            if (inputs[3])
            {
                inputDir.X -= 1;
            }
            if (inputs[4])
            {
                Console.WriteLine("Jump input recieved: //TODO: Implement jumping mechanic");
            }

            Move(inputDir);
        }

        public void Move(Vector2 inputDir)
        {
            Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

            Vector3 moveDir = right * inputDir.X + forward * inputDir.Y;
            position += moveDir * moveSpeed;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs = _inputs;
            rotation = _rotation;
        }
    }
}
