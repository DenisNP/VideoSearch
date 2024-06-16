export const getItemDescription = (item: any) => {
    let description = '';
    if (item.keywords && item.keywords.length) {
        description += '<p><strong>Видео:</strong> ' + item.keywords.join(', ') + '</p>';
    }
    if (item.sttKeywords && item.sttKeywords.length) {
        description += '<p><strong>Аудио:</strong> ' + item.sttKeywords.join(', ') + '</p>';
    }
    if (item.centroids && item.centroids.length) {
        description += '<p><strong>Центры кластеров:</strong> ' + item.centroids.join(', ') + '</p>';
    }
    return description;
};