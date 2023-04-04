using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;

namespace CleanUsersCardsData
{
    class Program
    {
        public static HttpClient Hclient = new HttpClient(); //один общий клиент чтобы избежать переполнения HTTP сокетов

        static async Task Main(string[] args)
        {
            var config = Config.GetConfig();

            var authenticationBytes = Encoding.UTF8.GetBytes($"{config.AdminName}:{config.AdminPass}");
            Hclient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));           
            Hclient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authenticationBytes));

            string ServerAdress = config.ServerName; //Console.ReadLine();
            string allUsersUri = "http://" + ServerAdress + $":{config.ServerPort}/api/v1/users";
            string SingleuserURI = "http://" + ServerAdress + $":{config.ServerPort}/api/v1/users/ID";

            string allUsers = await GetUsers(allUsersUri);
            if (allUsers == null)
            {
                Log("Cant connect to the Server");
            }
            else
            {
                allUsers = allUsers.Replace("$", "");//в получаемом JSON есть $id и $value с символом $ не получается десерилизовать 
                UserArray users = JsonSerializer.Deserialize<UserArray>(allUsers);

                Console.WriteLine($"All users Received, total : {users.values.Length}");

                List<string> Email = Sortjson.SortEmails(users);
                Console.WriteLine($"Double Emails detected {Email.Count}");

                List<string> Phones = Sortjson.SortPhones(users);
                Console.WriteLine($"Double Phones detected {Phones.Count}");

                List<string> ISQ = Sortjson.SortICQUINs(users);
                Console.WriteLine($"Double ICQUINs detected {ISQ.Count}");

                List<string> LocaUserDN = Sortjson.SortLocalUserDNs(users);
                Console.WriteLine($"Double LocalUserDN detected {LocaUserDN.Count}");

               
                List<string> SocialNetworkIDs = Sortjson.SortSocialNetworkIDs(users);
                Console.WriteLine($"Double SocialNetworkIDs detected {SocialNetworkIDs.Count}");

                List<string> SortSkypeAccounts = Sortjson.SortSkypeAccounts(users);
                Console.WriteLine($"Double SortSkypeAccounts detected {SortSkypeAccounts.Count}");

                List<string> SortTelegramAccounts = Sortjson.SortTelegramAccounts(users);
                Console.WriteLine($"Double TelegramAccounts detected {SortTelegramAccounts.Count}");

                List<string> SortYahooAccounts = Sortjson.SortYahooAccounts(users);
                Console.WriteLine($"Double YahooAccount detected {SortTelegramAccounts.Count}");

                var needToUpdate = SearchUser.SearchByEmail(users, Email, Phones, ISQ, LocaUserDN, SocialNetworkIDs, SortSkypeAccounts, SortTelegramAccounts, SortYahooAccounts);
                Console.WriteLine($"Users To need Update {needToUpdate.Count}");
                Console.WriteLine();
                Console.WriteLine();
                //========================================================================================================//


                foreach (var ID in needToUpdate) //проходимся по всем ID у которых в исходном json  со всеми пользователями заменяли поля
                {
                    // делаем запрос по ID для получения пользователя
                    SingleuserURI = "http://" + ServerAdress + $":{config.ServerPort}/api/v1/users/" + ID.ID.ToString();
                    string singlUsers = await GetSingleUsers(SingleuserURI);
                    singlUsers = singlUsers.Replace("$", "");//в получаемом JSON есть $id и $value с символом $ не получается десерилизовать
                    try
                    {
                        SinglUser sUser = JsonSerializer.Deserialize<SinglUser>(singlUsers);
                        if (sUser.GetType() != typeof(SinglUser)) //словил пару глюков, решил , что если не получается десериализовать в положенный вид пропускать 
                        {
                            Log($"Cant Serialize: {singlUsers}");
                            continue;
                        }
                        sUser.type = "FalconGaze.SecureTower.UsersServerAPI.Models.User, Falcongaze.SecureTower.UsersServerAPI"; //костыть из-зе непоняток с сериализацией  значения $type

                        var tempUser = (from p in users.values where p.ID == ID.ID select p).First(); //из всех пользователей берем того у кого ID нужный 


                        Log($"Update :{tempUser.FirstName} {tempUser.LastName}");
                        if (ID.Emails)
                        {
                            sUser.EMails = tempUser.EMails.values;
                        }
                        if (ID.ICQUINs)
                        {
                            sUser.ICQUINs = tempUser.ICQUINs.values;
                        }

                        if (ID.LocalUserDNs)
                        {
                            sUser.LocalUserDNs = tempUser.LocalUserDNs.values;
                        }
                        if (ID.Phones)
                        {
                            sUser.Phones = tempUser.Phones.values;
                        }
                        if (ID.SkypeAccounts)
                        {
                            sUser.SkypeAccounts = tempUser.SkypeAccounts.values;
                        }
                        if (ID.SocialNetworkIDs)
                        {
                            sUser.SocialNetworkIDs = tempUser.SocialNetworkIDs.values;
                        }
                        if (ID.TelegramAccounts)
                        {
                            sUser.TelegramAccounts = tempUser.TelegramAccounts.values;
                        }
                        if (ID.YahooAccounts)
                        {
                            sUser.YahooAccounts = tempUser.YahooAccounts.values;
                        }
                        try
                        {
                            string Fstring = JsonSerializer.Serialize<SinglUser>(sUser);
                            var testPut = PUTtSingleUsers(SingleuserURI, Fstring);
                           
                        }
                        catch (Exception)
                        {
                            Log("EXEPTION WHILE JsonSerializer.Serialize<SinglUser>(sUser)");
                            Log(ID.ID.ToString());
                            throw;
                        }
                    }
                    catch (Exception)
                    {
                        Log("Main Try Catch");

                    }
                    Thread.Sleep(100);

                }

            }
            Console.WriteLine("Completed, press any key for exit");
            Console.ReadLine();
            
        }

        public static async Task<string> PUTtSingleUsers(string SingleuserURI, string json)
        {      
            
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var result = await Hclient.PutAsync(SingleuserURI, content);
                var result_string = await result.Content.ReadAsStringAsync();
            Log("PUTtSingleUsers" + SingleuserURI);
            Log(result.StatusCode.ToString());
            return result_string;
            
        }

        public static async Task<string> GetSingleUsers(string SingleuserURI)
        {
            var result = await Hclient.GetAsync(SingleuserURI);
            var result_string = await result.Content.ReadAsStringAsync();
            Log("GetSingleUsers" + SingleuserURI);
           // Log(SingleuserURI);
            Log(result.StatusCode.ToString());
            return result_string;            
        }

        public static async Task<string> GetUsers(string URI)
        {
            try
            {
                var result = await Hclient.GetAsync(URI);
                Log(result.StatusCode.ToString());
                if (!result.IsSuccessStatusCode)
                {
                    Log(result.ReasonPhrase);
                    Console.WriteLine(result.ReasonPhrase);
                    return null;
                }
                var result_string = await result.Content.ReadAsStringAsync();
                Log("GetAllUsers");
                Log(result.StatusCode.ToString());
                return result_string;
            }
            catch (Exception ex)
            {

                Log(ex.GetType().FullName);
                return null;
            }
 
            
        }

            public static class Sortjson //методы получают всех пользователей и возвращают список элементов по которым нашлись дубли 
            {
                public static List<string> SortEmails(UserArray userArray)
                {
                    List<String> mails = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.EMails.values)
                        {
                            mails.Add(item);
                        }
                    }
                    mails.Sort();
                    List<string> SortList = new List<string>();
                    for (int i = 0; i < mails.Count-1 ; i++)
                    {
                        if (mails[i] == mails[i+1])
                        {
                            string mailForClear = mails[i];
                            SortList.Add(mails[i]);                       
                        
                            while (mailForClear == mails[i + 1] && i < mails.Count - 2)
                            {                            
                                i++;
                            }
                        }
                    }
                Data(SortList, "Emails");
                return SortList;               
                }


                public static List<string> SortPhones(UserArray userArray)
                {
                    List<String> phone = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.Phones.values)
                        {
                            phone.Add(item);
                        }
                    }
                phone.Sort();
                    List<string> phoneSortList = new List<string>();
                    for (int i = 0; i < phone.Count - 1; i++)
                    {
                        if (phone[i] == phone[i + 1])
                        {
                            string phoneForClear = phone[i];
                        phoneSortList.Add(phone[i]);
                        
                            while (phoneForClear == phone[i + 1] && i < phone.Count - 2)
                            {
                                i++;
                            }
                        }

                    }
                Data(phoneSortList, "Phones");
                return phoneSortList;
                }


                public static List<string> SortICQUINs(UserArray userArray)
                {
                    List<String> isq = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.ICQUINs.values)
                        {
                        isq.Add(item);
                        }
                    }
                isq.Sort();

                    List<string> isqSortList = new List<string>();
                    for (int i = 0; i < isq.Count - 1; i++)
                    {
                        if (isq[i] == isq[i + 1])
                        {
                            string mailForClear = isq[i];
                        isqSortList.Add(isq[i]);

                            while (mailForClear == isq[i + 1] && i < isq.Count - 2)
                            {
                                i++;
                            }
                        }

                    }
                Data(isqSortList, "ICQUIN");
                return isqSortList;
                }

                public static List<string> SortSkypeAccounts(UserArray userArray)
                {
                    List<String> skype = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.SkypeAccounts.values)
                        {
                        skype.Add(item);
                        }
                    }
                skype.Sort();
                    List<string> SkypeSortList = new List<string>();
                    for (int i = 0; i < skype.Count - 1; i++)
                    {
                        if (skype[i] == skype[i + 1])
                        {
                            string mailForClear = skype[i];
                        SkypeSortList.Add(skype[i]);

                            while (mailForClear == skype[i + 1] && i < skype.Count - 2)
                            {
                                i++;
                            }
                        }

                    }
                Data(SkypeSortList, "SkypeAccounts");
                return SkypeSortList;
                }


                public static List<string> SortYahooAccounts(UserArray userArray)
                {
                    List<String> Yahoo = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.YahooAccounts.values)
                        {
                        Yahoo.Add(item);
                        }
                    }
                Yahoo.Sort();
                    List<string> YahooSortList = new List<string>();
                    for (int i = 0; i < Yahoo.Count - 1; i++)
                    {
                        if (Yahoo[i] == Yahoo[i + 1])
                        {
                            string mailForClear = Yahoo[i];
                        YahooSortList.Add(Yahoo[i]);

                            while (mailForClear == Yahoo[i + 1] && i < Yahoo.Count - 2)
                            {
                                i++;
                            }
                        }

                    }
                Data(YahooSortList, "YahooAccounts");
                return YahooSortList;
                }
                public static List<string> SortLocalUserDNs(UserArray userArray)
                {
                    List<String> UserDNs = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.LocalUserDNs.values)
                        {
                        UserDNs.Add(item);
                        }
                    }
                UserDNs.Sort();
                    List<string> UserDNsSortList = new List<string>();
                    for (int i = 0; i < UserDNs.Count - 1; i++)
                    {
                        if (UserDNs[i] == UserDNs[i + 1])
                        {
                            string mailForClear = UserDNs[i];
                        UserDNsSortList.Add(UserDNs[i]);

                            while (mailForClear == UserDNs[i + 1] && i < UserDNs.Count - 2)
                            {
                                i++;
                            }
                        }

                    }
                Data(UserDNsSortList, "LocalUserDNs");
                return UserDNsSortList;
                }
                public static List<string> SortSocialNetworkIDs(UserArray userArray)
                {
                    List<String> netIDs = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.SocialNetworkIDs.values)
                        {
                        netIDs.Add(item);
                        }
                    }
                netIDs.Sort();
                    List<string> netIDsSortList = new List<string>();
                    for (int i = 0; i < netIDs.Count - 1; i++)
                    {
                        if (netIDs[i] == netIDs[i + 1])
                        {
                            string mailForClear = netIDs[i];
                        netIDsSortList.Add(netIDs[i]);

                            while (mailForClear == netIDs[i + 1] && i < netIDs.Count - 2)
                            {
                                i++;
                            }
                        }

                    }
                Data(netIDsSortList, "SocialNetworkIDs");
                return netIDsSortList;
                }


                public static List<string> SortTelegramAccounts(UserArray userArray)
                {
                    List<String> Telegram = new List<String>();

                    foreach (var user in userArray.values) //делаем списов всех емайлов
                    {
                        foreach (var item in user.TelegramAccounts.values)
                        {
                        Telegram.Add(item);
                        }
                    }
                Telegram.Sort();
                    List<string> TelegramSortList = new List<string>(); //сюда складываем те элементы по которым есть дубли
                    for (int i = 0; i < Telegram.Count - 1; i++)
                    {
                        if (Telegram[i] == Telegram[i + 1])
                        {
                            string mailForClear = Telegram[i];
                        TelegramSortList.Add(Telegram[i]);

                            while (mailForClear == Telegram[i + 1] && i < Telegram.Count - 2) //пропускаем одинаковые элементы
                            {
                                i++;
                            }
                        }

                    }
                Data(TelegramSortList, "TelegramAccounts");
                return TelegramSortList;
                }
            }
            public static class SearchUser {
                                                    public static List<NeedToUpdate> SearchByEmail(UserArray userArray, List<string> Email, List<string> Phones, List<string> ISQ, List<string> LocaUserDN, List<string> SocialNetworkIDs, List<string> SortSkypeAccounts, List<string> SortTelegramAccounts, List<string> SortYahooAccounts)
                                                        {
                                                            List<NeedToUpdate> needToUpdateList = new List<NeedToUpdate>();
                //Create a CSV Header 

                string SCVHeader = "Username;ID;SID;Email_Before;Email_After;Phones_Before;Phones_After;ICQ_Before;ICQ_After;Skype_Before;Skype_After;Yahoo_Before;Yahoo_After;LocalUserDN_Before;LocalUserDN_After;SocialNetworkID_Before;SocialNetworkID_After;Telegram_Before;Telegram_After\n\r";
                string CSVFilePath = Path.Join(AppContext.BaseDirectory, "ChangesByEachUser.csv");
                File.WriteAllText(CSVFilePath, SCVHeader);   
                
                                                                foreach (var user in userArray.values) {

                                                                NeedToUpdate allExeptDoubl = new NeedToUpdate();

                                                                List<string> mailToADD = new List<string>();
                                                                    foreach (var mail in user.EMails.values)
                                                                    {

                                                                        if (Email.Contains(mail)) // проверяем есть ли элемент в списке с задвоениями
                                                                        {
                                                                            allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                            allExeptDoubl.Emails = true;
                                                                              
                                                                            continue;
                                                                        }
                                                                        mailToADD.Add(mail); // складываем все что не задваивается                       
                                                                    }
                                                                    string[] mails = mailToADD.ToArray();

                                                                        List<string> phoneToADD = new List<string>();
                                                                        foreach (var phone in user.Phones.values)
                                                                        {

                                                                            if (Phones.Contains(phone)) // проверяем есть ли элемент в списке с задвоениями
                                                                            {
                                                                                allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                allExeptDoubl.Phones = true;
                                                                                continue;
                                                                            }
                                                                            phoneToADD.Add(phone); // складываем все что не задваивается                       
                                                                        }
                                                                        string[] phones = phoneToADD.ToArray();

                                                                            List<string> isqToADD = new List<string>();
                                                                            foreach (var isq in user.ICQUINs.values)
                                                                            {

                                                                                if (ISQ.Contains(isq)) // проверяем есть ли элемент в списке с задвоениями
                                                                                {
                                                                                    allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                    allExeptDoubl.ICQUINs = true;
                                                                                    continue;
                                                                                }
                                                                                isqToADD.Add(isq); // складываем все что не задваивается                       
                                                                            }
                                                                            string[] isquins = isqToADD.ToArray();

                                                                                List<string> locaUserDNToADD = new List<string>();
                                                                                foreach (var locaUserDN in user.LocalUserDNs.values)
                                                                                  {
                                                                                  if (LocaUserDN.Contains(locaUserDN)) // проверяем есть ли элемент в списке с задвоениями
                                                                                  {
                                                                                   allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                    allExeptDoubl.LocalUserDNs = true;
                                                                                   continue;
                                                                                   }
                                                                                    locaUserDNToADD.Add(locaUserDN); // складываем все что не задваивается                       
                                                                                    }
                                                                                string[] locaUserDNs = locaUserDNToADD.ToArray();

                                                                                    List<string> SocialNetworkID = new List<string>();
                                                                                    foreach (var socialNetworkID in user.SocialNetworkIDs.values)
                                                                                    {

                                                                                        if (SocialNetworkIDs.Contains(socialNetworkID)) // проверяем есть ли элемент в списке с задвоениями
                                                                                        {
                                                                                            allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                            allExeptDoubl.SocialNetworkIDs = true;
                                                                                            continue;
                                                                                        }
                                                                                    SocialNetworkID.Add(socialNetworkID); // складываем все что не задваивается                       
                                                                                    }
                                                                                    string[] socialNetworkIDs = SocialNetworkID.ToArray();


                                                                                    List<string> SortSkypeAccount = new List<string>();
                                                                                    foreach (var sortSkypeAccount in user.SkypeAccounts.values)
                                                                                        {

                                                                                            if (SortSkypeAccounts.Contains(sortSkypeAccount)) // проверяем есть ли элемент в списке с задвоениями
                                                                                            {
                                                                                                allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                                allExeptDoubl.SkypeAccounts= true;
                                                                                                continue;
                                                                                            }
                                                                                            SortSkypeAccount.Add(sortSkypeAccount); // складываем все что не задваивается                       
                                                                                        }
                                                                                        string[] sortSkypeAccounts = SortSkypeAccount.ToArray();



                                                                                        List<string> TelegramToADD = new List<string>();
                                                                                        foreach (var sortTelegramAccount in user.TelegramAccounts.values)
                                                                                            {

                                                                                                if (SortTelegramAccounts.Contains(sortTelegramAccount)) // проверяем есть ли элемент в списке с задвоениями
                                                                                                {
                                                                                                    allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                                    allExeptDoubl.TelegramAccounts = true;
                                                                                                    continue;
                                                                                                }
                                                                                        TelegramToADD.Add(sortTelegramAccount); // складываем все что не задваивается                       
                                                                                            }
                                                                                            string[] sortTelegramAccounts = TelegramToADD.ToArray();

                                                                                                List<string> YahooToADD = new List<string>();
                                                                                                foreach (var sortYahooAccount in user.YahooAccounts.values)
                                                                                                {

                                                                                                    if (SortYahooAccounts.Contains(sortYahooAccount)) // проверяем есть ли элемент в списке с задвоениями
                                                                                                    {
                                                                                                        allExeptDoubl.ID = user.ID; //берем ID пользователей у которых есть задвоения
                                                                                                        allExeptDoubl.YahooAccounts = true;
                                                                                                        continue;
                                                                                                    }
                                                                                                YahooToADD.Add(sortYahooAccount); // складываем все что не задваивается                       
                                                                                                }
                                                                                                string[] sortYahooAccounts = YahooToADD.ToArray();



                                                                if (allExeptDoubl.Emails|| allExeptDoubl.ICQUINs||allExeptDoubl.LocalUserDNs||allExeptDoubl.Phones||allExeptDoubl.SkypeAccounts||allExeptDoubl.SocialNetworkIDs) //если надо заменить 
                                                                    {
                                                                            using (StreamWriter sw = File.AppendText(CSVFilePath))
                                                                            {
                                                                         string toAdd = user.FirstName + ";" +user.ID + ";" + string.Join(",", user.SIDs.values) + ";" + string.Join(",", user.EMails.values) + ";";                                                  
                                                                        
                                                                            if (user.EMails.values.Length ==  mails.Length)
                                                                            {
                                                                                 toAdd += "No changes;";
                                                                            }
                                                                            else
                                                                                toAdd += string.Join(",", mails) + ";";
                                                                        user.EMails.values = mails; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках
                            //================================================================================================================================//


                                                                        toAdd += string.Join(",", user.Phones.values) + ";";   

                                                                        if (user.Phones.values.Length == phones.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", phones) + ";";
                                                                        user.Phones.values = phones; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 
                                                     //================================================================================================================================//

                                                                             toAdd += string.Join(",", user.ICQUINs.values) + ";";
                                                                    
                                                                        if (user.ICQUINs.values.Length == isquins.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", isquins) + ";";

                                                                                     user.ICQUINs.values = isquins; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 
                            //================================================================================================================================//
                                                                    toAdd += string.Join(",", user.SkypeAccounts.values) + ";";
                                                                        if (user.SkypeAccounts.values.Length== sortSkypeAccounts.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", sortSkypeAccounts) + ";";
                                                                          user.SkypeAccounts.values = sortSkypeAccounts; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 
                                         //================================================================================================================================//
                                                                             toAdd += string.Join(",", user.YahooAccounts.values) + ";";
                                                                        if (user.YahooAccounts.values.Length== sortYahooAccounts.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", sortYahooAccounts) + ";";
                                                                        user.YahooAccounts.values = sortYahooAccounts; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 

                            //================================================================================================================================//


                                                                        toAdd += string.Join(",", user.LocalUserDNs.values) + ";";
                                                                       
                                                                        if (user.LocalUserDNs.values.Length== locaUserDNs.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", locaUserDNs) + ";";
                                                                    user.LocalUserDNs.values = locaUserDNs; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 
                                                                    //================================================================================================================================//
                                                                         toAdd += string.Join(",", user.SocialNetworkIDs.values) + ";";
                                                                        if (user.SocialNetworkIDs.values.Length == sortYahooAccounts.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", socialNetworkIDs) + ";";
                                                                  user.SocialNetworkIDs.values = socialNetworkIDs; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 
                                   //================================================================================================================================//
                                                                         toAdd += string.Join(",", user.TelegramAccounts.values) + ";";
                                                                        if (user.TelegramAccounts.values.Length== sortYahooAccounts.Length)
                                                                        {
                                                                            toAdd += "No changes;";
                                                                        }
                                                                        else
                                                                            toAdd += string.Join(",", sortTelegramAccounts);
                                                                        user.TelegramAccounts.values = sortTelegramAccounts; //меняем значение в исходном документе , потом от туда по ID будем брать значения для замены в карточках 

                                                                         sw.WriteLine(toAdd);
                                                                     }
                                                                           needToUpdateList.Add(allExeptDoubl);// добавляем в список 
                                                                     }
                    
                                                            }
                                                            return needToUpdateList;
                                                        }
                                                    public class NeedToUpdate
                                                    {
                                                        public int ID { get; set; }
                                                        public bool   Emails { get; set; }
                                                        public bool Phones { get; set; }
                                                        public bool ICQUINs { get; set; }
                                                        public bool SkypeAccounts { get; set; }
                                                        public bool YahooAccounts { get; set; }
                                                        public bool LocalUserDNs { get; set; }
                                                        public bool SocialNetworkIDs { get; set; }
                                                        public bool TelegramAccounts { get; set; }
                                                    }

                                            }

        public  class Config : SeverConfig
        {

            static string configFileName = Path.Join(AppContext.BaseDirectory, "Config.json");

            public static SeverConfig GetConfig()            {
                string conf = @"{
                                ""ServerName"": ""localhost"",
                                ""ServerPort"": ""39001"",
                                ""AdminName"": ""admin"",
                                ""AdminPass"": ""123""
                                }";
                if (!File.Exists(configFileName))
                {
                    SeverConfig? severConfig = JsonSerializer.Deserialize<SeverConfig>(conf);
                    string temp = JsonSerializer.Serialize<SeverConfig>(severConfig);
                    File.WriteAllText(configFileName, temp);

                    Console.WriteLine($"Set the Server connection parameters to {configFileName}");
                    Console.WriteLine("Save the configuration file and press any key");
                    Console.ReadLine();
                    
                }

                SeverConfig? TT = JsonSerializer.Deserialize<SeverConfig>(File.ReadAllText(configFileName));
                return TT;
            }
            

        }

        public class SeverConfig
        {
            [JsonPropertyName("ServerName")]
            public  string ServerName { get; set; }
            [JsonPropertyName("ServerPort")]
            public  string ServerPort { get; set; }
            [JsonPropertyName("AdminName")]
            public  string AdminName { get; set; }
            [JsonPropertyName("AdminPass")]
            public  string AdminPass { get; set; }
        }

        public static void Log(string text)
        {
            DateTime now = DateTime.Now;

            string LogFileName = Path.Join(AppContext.BaseDirectory, $"LOG_{now.ToString("d")}.txt");
            using (StreamWriter sw = File.AppendText(LogFileName))
            {
                sw.WriteLine($"{now:G}: {text}");
            }
        }

        public static void Data(List<string> dataName, string DataFileName)
        {
            DateTime now = DateTime.Now;

            string LogFileName = Path.Join(AppContext.BaseDirectory, $"{now.ToString("d")}_{DataFileName}.txt");
            using (StreamWriter sw = File.AppendText(LogFileName))
            {
                foreach (var item in dataName)
                {
                    sw.WriteLine($"{item}");
                }
              
            }
        }



        public static void LogByUser(User user, string newValue, string username)
        {
            using (StreamWriter sw = File.AppendText(username))
            {
                sw.WriteLine($"{user.FirstName}| {user.ID} |{user.SIDs.values}");
                sw.WriteLine("Berfor");

                foreach (var item in user.EMails.values)
                {                    
                    sw.WriteLine($"{item}");
                }
                sw.WriteLine("After");
                sw.WriteLine(newValue);
            }
        }
    }

}
