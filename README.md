# Назначение
Система предназначена для загрузки информации о ремонтах (комплектах документов) с портала PortalName.net (реальное имя портала изменено по нонятным причинам).
Состав информации:
* XML описание ремонта
* Печатные формы (Акт)
* Документы ЭДО (СФ) - CФ

## Диаграмма прецедентов - загрузка информации с портала PortalName.net
![image](https://github.com/sansansan74/RvSfDownloadCore/assets/169544677/72b7bc0f-b2d1-44db-a43c-703c866c2f6f)
## Диаграмма прецедентов - сохранение информации в БД
Каждый блок соответствует логическома вызову хранимой процедуры
![image](https://github.com/sansansan74/RvSfDownloadCore/assets/169544677/c3d1a837-615e-46d6-937c-4d9a94c556a6)

# Используемые технологии
1. Исполняемый модуль - DOT NET CORE 6.0
2. БД - MS SQL SERVER 2019 и старше

# Архитектура системы
Система реализована как классический микросервис, функционирующий по схеме клиент-сервер.

# Диаграмма развертывания
![image](https://github.com/sansansan74/RvSfDownloadCore/assets/169544677/b2070da9-515d-4fc3-99af-2dce3000810b)


# Краткий алгоритм работы
* Система периодически обращается к порталу PortalName.net и забирает краткую информацию об обновлениях комплектов документов. На основе анализа краткой  информации система понимает, есть ли необходимость обновлять подробную информацию о комплекте документов.
* Если есть, то система выгружает полную информацию об обновлении в формате XML и сохраняет в БД.
* Если создались или обновились подчиненные документы в комплекте документов, то система выгружает их и сохраняет в БД
* Также система отслеживает откаты - ситуации, когда у ремонта была подписанная СФ ЭДО, а потом документ вернулся на этап "не согласовано вагончиком"

# Диаграмма обработки откатов
![image](https://github.com/sansansan74/RvSfDownloadCore/assets/169544677/a2cfbe17-746f-48d1-9014-b33b214a1feb)

# Требования к установке
* Наличие БД MS SQL Server версии 2019
* Наличие Windows сервера или Linux сервера

# Инструкции для разворачивания на Windows Server
1. Развернуть БД rv_document_store на MS SQL сервер
1. Дать учетной записи права на схему WEB на чтение и выполнение (аутенцификация м.б. как Windows, так и SQL Server)
1. Скопировать файлы дистрибутива в папку назначения на сервер
2. выдать права пользователю, под которым будет запущен сервис в ОС, на чтение папки проекта и запись в папку логов
3. Осуществить настройку файла настроек appsettings.json
4. Осуществить настройку файла настроек логгирования nlog.config (библиотека nlog, гуглятся настройки)
5. настроить запуск по расписанию раз в 30 минут: сделать задачу в TaskManager в Windows или в cron в Linux

# Инструкции для разворачивания на Linux Server
1. Развернуть БД rv_document_store на MS SQL сервер
1. Дать учетной записи права на схему WEB на чтение и выполнение (аутенцификация д.б. только SQL Server!)
1. Скопировать файлы дистрибутива в папку назначения на сервер
2. выдать права пользователю, под которым будет запущен сервис в ОС, на чтение папки проекта и запись в папку логов, а также на исполнение исполняемого файла
3. Осуществить настройку файла настроек appsettings.json
4. Осуществить настройку файла настроек логгирования nlog.config (библиотека nlog, гуглятся настройки)
5. настроить в cron запуск по расписанию раз в 30 минут:
6. - crontab -e
7. - */30 * * * * cd /microservices/RvActAutoCommiter && ./RvActAutoCommiter

Если не написать cd /microservices/RvActAutoCommiter && ./RvActAutoCommiter, а указать просто имя скрипта, то он не находит домашней папки и не может считать файл appsettings.json .

# Настройки
Проставить в файле настроек:
1. Строку соединения с БД
  "ConnectionStrings": {
    "SfStoreConnect": "Server=WINDOWS_SERVER;Database=rv_document_store;Trusted_Connection=true;"
    }
  
## Учетные записи и права

* Дать учетной записи права на схему WEB на чтение и выполнение
* выдать права пользователю, под которым будет запущен сервис в ОС, на чтение папки проекта и запись в папку логов
* необходимо иметь возможность пользователю, под которым запущен сервис, осуществлять доступ к https://PortalName.net

## От каких подсистем зависит - какие подсистемы использует
* от системы PortalName.net

# Исходные коды диаграмм
Диаграммы реализованы на языке разметки PlantUML. Исходные коды диаграмм хранятся в папке Doc. Сервер, на котором можно сгенерировать изображения диаграмм по их исходному тексту: plantuml.com

# Диаграмма БД
![db_diagram](https://github.com/sansansan74/RvSfDownloadCore/assets/169544677/267a576c-f611-40de-b1b3-0002b0b4041c)

# Устройсво репозитория
* src - исходные коды
* srс\csharp - код микросервиса загрузки данных с портала
* src\sql - код на MS SQL Server, где хранятся загруженные данные с портала
* doc - папка с документацией. Хранит изображение диаграмм на PlantUml и скриншот диаграммы БД
