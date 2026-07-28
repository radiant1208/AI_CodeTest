import { apiClient } from './apiClient'

/** MapDirectory에 있는 맵 파일명 목록을 조회한다. */
export async function getMaps(): Promise<string[]> {
  const response = await apiClient.get<string[]>('/maps')
  return response.data
}

/** 맵 원본 이미지를 canvas에 그리기 위한 URL. */
export function mapImageUrl(fileName: string): string {
  return `/api/maps/${encodeURIComponent(fileName)}`
}
