# Video Search
Демонстрационный сервис для индексации и поиска на русском языке по содержимому видеозаписей, делался для хакатона [Лига Цифровой Трансформации 2024](https://i.moscow/lct)

# Описание директорий и контейнеров
## PLLaVa API
WebAPI на `Python` для модели [PLLaVa](https://pllava.github.io/), позволяющей задавать вопросы о видео на английском языке. Должна запускаться на отдельном сервере с GPU.

## GigaAM API
WebAPI на `Python` для модели [GigaAm](https://github.com/salute-developers/GigaAM), позволяющей проводить транскрибацию (speech-to-text) голоса на русском языке. Должна запускаться на отдельном сервере с GPU.

## Navec
Одна из таблиц БД предварительно заполнена векторами из проекта [Navec](https://github.com/natasha/navec) для работы с семантической близостью. 

## LibreTranslate
Self-hosted API [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) для перевода на разные языки. Модели для русского и английского запускаются в общей сборке на одном сервере с бэкендом и фронтендом.

## video-search-frontend
Проект на `Vue3`, собирается в одном контейнере с бэкендом и раздаётся им же.

![image](https://github.com/DenisNP/VideoSearch/assets/720975/bee27abf-fd3c-4e04-abd7-62df28ea6b30)

## VideoSearchBackend
Серверное приложение на `ASP.NET Core`, `C#`, реализующее бизнес-логику по индексации, кластеризации, хранению и поиску. Собирается из `Dockerfile` в соответствующей директории.
