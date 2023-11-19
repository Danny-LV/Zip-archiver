using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using wf = System.Windows.Forms;

namespace ZipArch
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 
    public class IntWrapper
    {
        public int Value { get; set; }
    }
    public partial class MainWindow : Window
    {
        string zipPath, filePath; // Переменные для пути к файлу и архиву
        string info = "";
        IntWrapper progress = new IntWrapper(); // This variable will be used by different threads
        List<string> fileList = new List<string>(); // создаем список для файлов
        DispatcherTimer timer; // timer for ProgressBar

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           wf.OpenFileDialog ofd = new wf.OpenFileDialog(); // Создаем диалог для выбора файлов
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == wf.DialogResult.OK)
            {
                foreach (string fname in ofd.FileNames)
                    fileList.Add(fname); // Добавляем выбранные файлы в наш список

                string filename = ofd.FileName;
                filePath = filename;
                lb_file.Content = filename;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            wf.SaveFileDialog sfd = new wf.SaveFileDialog();
            sfd.OverwritePrompt = false; // Чтобы не спрашивал, что архив уже есть
            if (sfd.ShowDialog()== wf.DialogResult.OK)
            {
                string filename = sfd.FileName;
                zipPath = filename;
                lb_arch.Content = filename;
            }
        }

        async private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            /*lock (info)
            {
                tb_inf.Text = info;
            }*/
            tb_inf.Text = "Archiving........";
            // создание сжатого файла
            await Task.Run(() => ArchFile(zipPath, fileList));
            // Запускаем метод архивации файлов в параллельном потоке
            tb_inf.Text = "Successfully archived!";
        }

        void ArchFile(string zipPath, List<string> fileList)
        {
            int m = 0;
            int maxfl = fileList.Count;
            using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
            {
                foreach (string fname in fileList)
                {
                    //string path = Path.GetFullPath(fname); // Got the file path
                    string name = Path.GetFileName(fname); // Got the file name
                    archive.CreateEntryFromFile(fname, name); // Добавляем файлы в ZIP-архив
                    m++;
                    lock(progress)
                    {
                        progress.Value = m * 100 / maxfl;
                    }
                }
            }
        }

        void ExtrFile(string zipPath, string filePath)
        {
            int m = 0;

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                int maxfl = archive.Entries.Count; // Получаем сколько файлов находятся в архиве
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(filePath, entry.FullName));
                    //if (destinationPath.StartsWith(filePath, StringComparison.Ordinal))
                    m++;
                    if (File.Exists(destinationPath)) // Проверяем если выбранный файл уже существует
                    // то спрашиваем пользователя перезаписать его или нет
                    { 
                        if (MessageBox.Show($"файл {entry.FullName} существует," +
                        $" перезаписать?", "Перезапись", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            File.Delete(destinationPath); // Удаляем выбранный файл
                            entry.ExtractToFile(destinationPath);// Извлекаем файл из архива
                        }
                    }else
                    {
                        entry.ExtractToFile(destinationPath);
                    }
                    
                    lock (progress) // Closing the variable for other threads
                    {
                        progress.Value = m * 100 / maxfl;
                    }
                  
                }
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.IsEnabled = true;
            timer.Tick += On_Timer;
        }

        async private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderdg = new FolderBrowserDialog();
            // Выбираем в какую папку мы хотим поместить файлы
            folderdg.ShowNewFolderButton = true;
            DialogResult result = folderdg.ShowDialog();
            if (result == wf.DialogResult.OK)
            {
                filePath = folderdg.SelectedPath;
                
            }else
            {
                return;
            }

            tb_inf.Text = "Unzipping........";
            await Task.Run(() => ExtrFile(zipPath, filePath));
            // Запускаем метод разархивации файлов в параллельном потоке
            tb_inf.Text = "Successfully unzipped!";
        }

        private void On_Timer(object sender, EventArgs e)
        {
            int p = 0;
            lock(progress)
            {
                p = progress.Value;
            }
            prb.Value = p;
        }

    }
}
