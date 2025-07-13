using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestProject
{
    public static class MessageBoxService
    {
        /// <summary>
        /// Выводит сообщение об ошибке
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void OutError(string message, bool isVisible = true)
        {
            if (isVisible)
                MessageBox.Show(
                   message,
                   "Ошибка",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error);
        }

        /// <summary>
        /// Выводит сообщение об ошибке
        /// </summary>
        /// <param name="ex">Ошибка</param>
        public static void OutError(Exception ex, bool isVisible = true)
        {
            if (isVisible)
                MessageBox.Show(
                   string.Join("; ","Lox"),
                   "Ошибка",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error);
        }

        /// <summary>
        /// Выводит сообщение об ошибке
        /// </summary>
        /// <param name="ex">Ошибка</param>
        /// <param name="message">Дополнительное сообщение</param>
        public static void OutError(Exception ex, string message, bool isVisible = true)
        {
            if (isVisible)
                MessageBox.Show(
                   message + "\n" + string.Join("; ", "Lox"),
                   "Ошибка",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error);
        }

        /// <summary>
        /// Выводит информационное сообщение
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static void OutInformation(string message, bool isVisible = true)
        {
            if (isVisible)
                MessageBox.Show(
                   message,
                   "Информация",
                   MessageBoxButton.OK,
                   MessageBoxImage.Information);
        }

        /// <summary>
        /// Выводит вопрос
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static bool AskQuestion(string message)
        {
            var result = MessageBox.Show(
               message,
               "Вопрос",
               MessageBoxButton.YesNo,
               MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }
    }
}
