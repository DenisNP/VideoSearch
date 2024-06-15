export const getItemDescription = (item: any) => {
    let description = '';
    if (item.keywords && item.keywords.length) {
        description += '<p>Видео: <i>' + item.keywords.join(', ') + '</i></p>';
    }
    if (item.stt && item.stt.length) {
        description += '<p>Аудио: <i>' + item.stt.join(', ') + '</i></p>';
    }
    if (item.centroids && item.centroids.length) {
        description += '<p>Центры кластеров: <i>' + item.centroids.join(', ') + '</i></p>';
    }
    return description;
};