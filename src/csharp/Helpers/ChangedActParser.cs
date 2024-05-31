using RvSfDownloadCore.Util;
using System.Xml.Linq;

namespace RvSfDownloadCore.Services
{
    /// <summary>
    /// Класс разбивает все входящие теги ВАГОН на согласованные вагонником и экономистом и несогласованные
    /// </summary>
    class ChangedActParser
    {
        /// <summary>
        /// Согласованные
        /// </summary>
        public XElement Reconcilated { get; private set; }

        /// <summary>
        /// Не согласованные
        /// </summary>
        public XElement NonReconcilated { get; private set; }

        /// <summary>
        /// Создает xml с командами обновления в БД
        /// </summary>
        /// <param name="x">Исходный xml</param>
        /// <returns></returns>
        public void Parse(XElement changedVagons)
        {
            /*
            По xml вида: 
                <ЭКСПОРТ ВЕРСИЯ="4">
                    <Вагон Код="16300031296" Изменен="11.07.2019 09:56:32" .... >
                    <Вагон Код="16300031296" Изменен="11.07.2019 09:56:32" .... >
                </ЭКСПОРТ>
            или
            <ЭКСПОРТ ВЕРСИЯ="1">
                    <Документ Код="16300031296" Изменен="11.07.2019 09:56:32" .... >
                    <Документ Код="16300031296" Изменен="11.07.2019 09:56:32" .... >
            </ЭКСПОРТ>

            Создает xml вида:
                <root>
                    <act id="1" changed="20190711 09:56:32" />
                    <act id="2" changed="20190711 09:56:32" />
                </root>

                Тег:
                    ЭКСПОРТ ==> root
                    Вагон ==> act

                Атрибут Вагон:
                    Код ==> id
                    Изменен ==> changed , дата конвертируется в формат, пригодный для распознавания MS SQL
             */


            Reconcilated = new XElement("root");
            NonReconcilated = new XElement("root");

            foreach (var node in changedVagons.Elements())
            {
                if (isReconcilated(node))
                    Reconcilated.Add(CreateXelementAct(node));
                else
                    NonReconcilated.Add(CreateXelementActWithCheckMask(node));
            }

        }

        /// <summary>
        /// Проверяет, согласован ли вагонником и экономистом документ
        /// </summary>
        /// <param name="node">Узел ВАГОН</param>
        /// <returns>true, если документ согласован</returns>
        static bool isReconcilated(XElement node)
        {
            // Документ согласован, если маска состоит из 1 и #. И она не пуста
            string mask = (node.Attribute("Проверка")?.Value ?? string.Empty).Trim();

            if (mask.Length == 0)
                return false;

            foreach (char c in mask)
            {
                if (c != '1' && c != '#')
                    return false;
            }

            return true;
        }


        private static XElement CreateXelementAct(XElement node) =>
            new XElement(
                "act",
                new XAttribute("id", node.Attribute("Код").Value),
                new XAttribute("changed", node.Attribute("Изменен").Value.RussianDate2MsSqlDate())
            );

        private static XElement CreateXelementActWithCheckMask(XElement node) =>
            new XElement(
                "act",
                new XAttribute("id", node.Attribute("Код").Value),
                new XAttribute("changed", node.Attribute("Изменен").Value.RussianDate2MsSqlDate()),
                new XAttribute("checkMask", node.Attribute("Проверка").Value)
            );
    }
}
