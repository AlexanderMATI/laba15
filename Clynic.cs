using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;


namespace laba15
{

    enum Params
    {
        OrdinNum = 1,
        DocNum = 2,
        UpdatePeriod = 3,
        MaxTime = 4,
        PatientCome = 5,
        DP = 6,
        Stats = 7,
        StatPeople = 8,
        ToFile = 9,
        FileOut = 10
    }
    class Clynic
    {
        private static SemaphoreSlim _ReadyDoctors;
        private static SemaphoreSlim _OrRoom;

        private static Queue<Patients> _Patients = new Queue<Patients>();
        private static List<Patients> _WithDoctorsPatients = new List<Patients>();
        private static List<Patients> _InOrdinatoryWaitingPatients = new List<Patients>();

        private static Random Rand = new Random();

        /*
         * Изменяемые параметры
         * */
        private static int _MaxTime = 10; //Максимальное время, которое может словить больной
        private static int _MaxNumOfDoctors = 10; //Число врачей
        private static int _MaxNumInOrRoom = 12; //Максимальное число мест в смотровой
        
        private static int _DrawUpdatePeriod = 1000; //Период перерисовки
        private static int _MaxQueueDrawHead = 10; //Верхушка очереди для отрисовки

        private static double _PatientComeProbability = 0.2; //Вероятность прихода нового пациента в очередь
        private static double _DoctorNHFProbability = 0.2; //Вероятность того, что доктору понадобится помощь другого доктора

        private static bool _ShowStats = true; //Использовать ли наглядный режим отображения статистики



        
        private static int _DrawIterations = 0;  //Число итераций
        private static int _AllPatients = 0; //Сумма всех пациентов
        private static bool _IsSickInOrdinatory = false; //Есть ли в смотровой больные
        private static int _StartNumOfHumans = 10;
        private static string _OutputFile = "log.txt";
        private static bool _WriteLogToFile = false;

        private static StreamWriter _OutputWriter;

        public static void InitializeParams(Dictionary<Params, object> Parametres)
        {
            StringBuilder StartingInfo = new StringBuilder();
            foreach (var Element in Parametres)
            {
                StartingInfo.Append($" {Element.Key} -> {Element.Value}\n");
                

                switch (Element.Key)
                {
                    case Params.DocNum:
                        _MaxNumOfDoctors = Math.Abs((int)Element.Value);
                        break;
                    case Params.StatPeople:
                        _StartNumOfHumans = Math.Abs((int)Element.Value);
                        break;
                    case Params.ToFile:
                        _WriteLogToFile = (bool)(Element.Value);
                        break;
                    case Params.MaxTime:
                        _MaxTime = Math.Abs((int)Element.Value);
                        break;
                    case Params.DP:
                        _DoctorNHFProbability = Math.Abs((double)Element.Value);
                        break;
                    case Params.Stats:
                        _ShowStats = (bool)Element.Value;
                        break;
                    case Params.UpdatePeriod:
                        _DrawUpdatePeriod = Math.Abs((int)Element.Value);
                        break;
                    case Params.PatientCome:
                        _PatientComeProbability = Math.Abs((double)Element.Value);
                        break;
                    case Params.OrdinNum:
                        _MaxNumInOrRoom = Math.Abs((int)Element.Value);
                        break;
                    
                    case Params.FileOut:
                        _OutputFile = (string)(Element.Value);
                        break;

                    default:
                        break;

                }
                
            }

            if (_WriteLogToFile)
            {
                try
                {
                    if (_OutputFile.Length != 0)
                        _OutputWriter = File.AppendText(_OutputFile);

                }
                catch (IOException FileOpenError)
                {
                    Print("Не могу открыть файлик", true);
                    Print(FileOpenError.Message, true);
                    return;
                }

            }

            Print($"Запустим со следующими значениями: {StartingInfo.ToString()}", true);

            //Иницилизируем людей в очереди
            for (int i = 0; i < _StartNumOfHumans; ++i)
            {
                Clynic.PatientCome();
            }


            Thread.Sleep(2000);
        }

        public static void PatientCome()
        {
            Patients NewPatient = new Patients();
            _Patients.Enqueue(NewPatient);

            Print($"Пациент: {NewPatient.GetName()}, Больной: {NewPatient.GetIsSick()}");
            _AllPatients++;
        }

        public static void RandomPatientCome() 
        {
            if (Rand.NextDouble() > 1.0 - _PatientComeProbability)
            {
                PatientCome();
            }
        }

        public static int NumInOrdinatory()
        {
            return _MaxNumInOrRoom - _OrRoom.CurrentCount;
        }

        public static int NumOfDoctors()
        {
            return _MaxNumOfDoctors - _ReadyDoctors.CurrentCount;
        }
        public static void ModelQueue()
        {
            int Iterations = 0;

            while (true)
            {
                if (++Iterations % 1000000 == 0)
                {


                    if (_ShowStats)
                    {
                        PrintStats();
                    }

                    if (_Patients.Count != 0)
                    {
                        //Берём текущего пациента в очереди
                        Patients CurPatient = _Patients.Peek();
                        
                        if (CurPatient.GetIsSick()) //Если пациент болен, то
                        {
                            if (_IsSickInOrdinatory || NumInOrdinatory() + NumOfDoctors() == 0) //Если в смотровой уже есть больные пациенты или ординаторская пустая, то добавляемся туда
                            {
                                if (NumInOrdinatory() < _MaxNumInOrRoom)
                                {
                                    _IsSickInOrdinatory = true;
                                    Thread thread = new Thread(new ParameterizedThreadStart(InOrdinator));
                                    thread.Start(CurPatient);
                                    _Patients.Dequeue();

                                }
                            }
                        }
                        else //Если пациент здоров
                        {
                            if (!_IsSickInOrdinatory && NumInOrdinatory() < _MaxNumInOrRoom) //Если нет больных внутри и есть свободные места
                            {
                                Thread thread = new Thread(new ParameterizedThreadStart(InOrdinator));
                                thread.Start(CurPatient);
                                _Patients.Dequeue();
                            }
                        }

                    }
                    else
                    {
                        if (NumOfDoctors() == 0 && NumInOrdinatory() == 0)
                            break;
                    }

                }
                
                if (Iterations+1 % 10000 * 1200 == 0)
                {
                    RandomPatientCome();
                }
            }
            PrintStats(true);

            if (_WriteLogToFile)
                _OutputWriter.Close();
        }

        public static void StartProcess()
        {
            _ReadyDoctors = new SemaphoreSlim(0, _MaxNumOfDoctors); //Число врачей описывают первый семафор
            _OrRoom = new SemaphoreSlim(0, _MaxNumInOrRoom); //Места в смотровой описывают второй семафор

            Thread QueueCheck = new Thread(ModelQueue);

            QueueCheck.Start();

            _OrRoom.Release(_MaxNumInOrRoom);
            _ReadyDoctors.Release(_MaxNumOfDoctors);

            QueueCheck.Join();

        }

        public static void PrintStats(bool ShouldClear = false)
        {
            try
            {
                if (_DrawIterations++ % _DrawUpdatePeriod == 0 || ShouldClear)
                    Console.Clear();

                Console.SetCursorPosition(0, 0);

                Patients HeadOfQueue;
                if (_Patients.Count == 0)
                    HeadOfQueue = null;
                else
                    HeadOfQueue = _Patients.Peek();



                int UsedElents = 0;


                StringBuilder MetricaReport = new StringBuilder();



                MetricaReport.Append("Статистика: \n" +
                    $"Прошло итераций: {_DrawIterations}\n" +
                    $"В очереди: {_Patients.Count}\n" +
                    $"Доступно смотровых: {_OrRoom.CurrentCount}/{_MaxNumInOrRoom}\n" +
                    $"Доступно докторов: {_ReadyDoctors.CurrentCount}/{_MaxNumOfDoctors}\n" +
                    $"Всего пациентов: {_AllPatients}\n" +
                    $"Больных в смотровой: {_IsSickInOrdinatory}\n" +
                    $"Верхушка очереди: {((HeadOfQueue == null) ? "None" : HeadOfQueue.GetName())}, {((HeadOfQueue == null) ? false : HeadOfQueue.GetIsSick())}\n");

                
                MetricaReport.Append($"\n Пациенты ждущие в смотровой \n");

                foreach (Patients pac in _InOrdinatoryWaitingPatients)
                {
                    MetricaReport.Append($"{pac.GetName()}  -----> isSeek: {pac.GetIsSick()} \n");
                }

                MetricaReport.Append($"\nПациент с доктором\n");

                foreach (Patients pac in _WithDoctorsPatients)
                {
                    MetricaReport.Append($"{pac.GetName()}  -----> isSeek: {pac.GetIsSick()} \n");
                }

                MetricaReport.Append($"\n Пациента очередь:e [в верхушке {_MaxQueueDrawHead}]\n");

                foreach (Patients pac in _Patients)
                {
                    MetricaReport.Append($"{pac.GetName()}Болен:: {pac.GetIsSick()} \n");

                    if (++UsedElents == _MaxQueueDrawHead)
                        break;
                }

                Console.Write(MetricaReport.ToString());
            } catch (InvalidOperationException e)
            {
                Print("ошибочка");
            } 
        }

        public static void Print(string str, bool IgnoreStats = false)
        {
            if (!_ShowStats || IgnoreStats)
            {
                Console.WriteLine(str);
            }

            if (_WriteLogToFile)
            {
                try
                {
                    _OutputWriter.WriteLine(str);
                } catch (IOException FileException)
                {

                } 
            }
        }

        /*
         Метод, который описывает вход пациента в ординаторскую.
         Суть - заходит пациент и начинает выжидать доктора.
         Как только находится свободный доктор - пациент его сразу перехватывает.

            Короче, в этом методе надо реализовать 

        */
        private static void InOrdinator(Object patient)
        {
            Patients converted = (Patients) patient;
       
            _OrRoom.Wait();

            //Зашел в ординаторскую, занял своё место


            Print($"Вход в ординаторскую {converted.GetName()} Он в очереди и ожидает когда явится доктор");

            _InOrdinatoryWaitingPatients.Add(converted);

            _ReadyDoctors.Wait(); //Ждёт, когда освободится какой-нибудь врач

            if (_InOrdinatoryWaitingPatients.Count != 0)
                _InOrdinatoryWaitingPatients.Remove(converted);

            _WithDoctorsPatients.Add(converted);

            Print($"Доктор зашел в {converted.GetName()}");

            int NeedTime = Rand.Next(3, _MaxTime) * 1000;

            Print($"Доктору надобно {NeedTime / 1000} секунд для {converted.GetName()}");

            Thread.Sleep(NeedTime);

            if (Rand.NextDouble() > 1.0 - _DoctorNHFProbability)
            {
                _ReadyDoctors.Wait();

                int NextNeedTime = Rand.Next(3, _MaxTime) * 1000;
                Print($"Полное время: {NextNeedTime / 1000}  секунд для {converted.GetName()}");
                Thread.Sleep(NextNeedTime);

                _ReadyDoctors.Release();
            }

            Print($"Доктор таки завершил его работу для пациента: {converted.GetName()}");

            if (_WithDoctorsPatients.Count != 0)
                _WithDoctorsPatients.Remove(converted);
            
            _ReadyDoctors.Release();

            Print($"Ушли из смотровой {converted.GetName()} ");

            

            _OrRoom.Release();

            if (NumInOrdinatory()+NumOfDoctors() == 0)
                _IsSickInOrdinatory = false;

            RandomPatientCome();
        }
        
    }
}
