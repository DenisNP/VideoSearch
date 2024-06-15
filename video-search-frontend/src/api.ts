const address = 'https://videosearch.justanother.app';

export const getQueue = async (count: number) => {
    const result = await fetch(`${address}/api/GetQueue?count=${count}`);
    return await result.json();
}

export const addToIndex = async (url: string) => {
    const result = await fetch(`${address}/validation-api/index`, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({link: url, description: '' })
    });
    return result.status === 200;
}

export const hints = async (q: string) => {
    const result = await fetch(`${address}/api/Hints?q=${q}`);
    return await result.json();
}

export const search = async (q: string) => {
    const result = await fetch(`${address}/api/Search?q=${q}`);
    return await result.json();
}