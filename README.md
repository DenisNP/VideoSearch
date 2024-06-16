# Video Search
Демонстрационный сервис для индексации и поиска на русском языке по содержимому видеозаписей, делался для хакатона [Лига Цифровой Трансформации 2024]([I.moscow/lct](https://i.moscow/lct))

# Описание директорий
## PLLaVa API
WebAPI на `Python` для модели [PLLaVa](https://pllava.github.io/), позволяющей задавать вопросы о видео на английском языке. Должна запускаться на отдельном сервере с GPU.

## GigaAM API
WebAPI на `Python` для модели [GigaAm](https://github.com/salute-developers/GigaAM), позволяющей проводить транскрибацию (speech-to-text) голоса на русском языке. Должна запускаться на отдельном сервере с GPU.

## Navec API
WebAPI на `Python` для работы с векторами семантической близости из проекта [Navec](https://github.com/natasha/navec). Запускается в общей сборке на одном сервере с бэкендом и фронтендом.

## LibreTranslate
Self-hosted API [LibreTranslate](https://github.com/LibreTranslate/LibreTranslate) для перевода на разные языки. Модели для русского и английского запускаются в общей сборке на одном сервере с бэкендом и фронтендом.

## video-search-frontend
Проект на `Vue3`, собирается в одном контейнере с бэкендом и раздаётся им же.

![image](https://github.com/DenisNP/VideoSearch/assets/720975/bee27abf-fd3c-4e04-abd7-62df28ea6b30)

## VideoSearchBackend
Серверное приложение на `ASP.NET Core`, `C#`, реализующее бизнес-логику по индексации, кластеризации, хранению и поиску. Собирается из `Dockerfile` в соответствующей директории.
