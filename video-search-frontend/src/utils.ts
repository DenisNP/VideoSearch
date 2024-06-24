export const getItemDescription = (item: any) => {
    let description = '';
    if (item.keywords && item.keywords.length) {
        description += '<p><strong>Видео:</strong> ' + item.keywords.join(', ') + '</p>';
    }
    if (item.sttKeywords && item.sttKeywords.length) {
        description += '<p><strong>Аудио:</strong> ' + item.sttKeywords.join(', ') + '</p>';
    } else if (item.status === 99) {
        description += '<p><strong>Аудио:</strong> <i>(русская речь отсутствует)</i></p>';
    }
    if (item.centroids && item.centroids.length) {
        description += '<p><strong>Центры кластеров:</strong> ' + item.centroids.join(', ') + '</p>';
    }
    return description;
};