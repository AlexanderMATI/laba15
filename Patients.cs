using System;

namespace laba15
{
    class Patients
    {
        private bool _isSick = false;

        private readonly string _name;

        public bool GetIsSick()
        {
            return _isSick;
        }

        public void SetIsSick(bool value)
        {
            _isSick = value;
        }

        public string GetName()
        {
            return _name;
        }

        public Patients()
        {
            _name = NameRandom.NameRand();

            if (new Random().Next(0,10) > 6)
            {
                _isSick = true;
            }
        }

    }
}
